﻿using System.Collections.Generic;
using System.Linq;
using ActionStreetMap.Core;
using ActionStreetMap.Core.MapCss.Domain;
using ActionStreetMap.Core.Scene.Buildings;
using ActionStreetMap.Core.Tiling.Models;
using ActionStreetMap.Core.Unity;
using ActionStreetMap.Explorer.Geometry;
using ActionStreetMap.Explorer.Geometry.Utils;
using ActionStreetMap.Explorer.Helpers;
using ActionStreetMap.Explorer.Scene.Buildings.Facades;
using ActionStreetMap.Explorer.Scene.Buildings.Roofs;
using ActionStreetMap.Explorer.Utils;
using ActionStreetMap.Infrastructure.Dependencies;
using ActionStreetMap.Infrastructure.Reactive;
using ActionStreetMap.Maps.Helpers;
using UnityEngine;

namespace ActionStreetMap.Explorer.Scene
{
    /// <summary> Provides logic to build buildings. </summary>
    public class BuildingModelBuilder : ModelBuilder
    {
        private readonly IElevationProvider _elevationProvider;
        private readonly IEnumerable<IFacadeBuilder> _facadeBuilders;
        private readonly IEnumerable<IRoofBuilder> _roofBuilders;

        /// <inheritdoc />
        public override string Name { get { return "building"; } }

        /// <summary> Creates instance of <see cref="BuildingModelBuilder"/>. </summary>
        [Dependency]
        public BuildingModelBuilder(IElevationProvider elevationProvider,
                                    IEnumerable<IFacadeBuilder> facadeBuilders, 
                                    IEnumerable<IRoofBuilder> roofBuilders)
        {
            _elevationProvider = elevationProvider;

            _facadeBuilders = facadeBuilders.ToArray();
            _roofBuilders = roofBuilders.ToArray();
        }

        /// <inheritdoc />
        public override IGameObject BuildArea(Tile tile, Rule rule, Area area)
        {
            base.BuildArea(tile, rule, area);
            return BuildBuilding(tile, rule, area, area.Points);
        }

        private IGameObject BuildBuilding(Tile tile, Rule rule, Model model, List<GeoCoordinate> footPrint)
        {
            var points = ObjectPool.NewList<MapPoint>();
            
            //var simplified = ObjectPool.NewList<MapPoint>();

            PointUtils.GetClockwisePolygonPoints(_elevationProvider, tile.RelativeNullPoint, footPrint, points);
            var minHeight = BuildingRuleExtensions.GetMinHeight(rule);

            // NOTE simplification is important to build hipped/gabled roofs
            //PolygonUtils.Simplify(points, simplified);

            var elevation = points.Average(p => p.Elevation);

            if (tile.Registry.Contains(model.Id))
                return null;

            var gameObject = BuildGameObject(tile, rule, model, points, elevation, minHeight);

            ObjectPool.StoreList(points);
            //ObjectPool.StoreList(simplified);

            return gameObject;
        }

        private IGameObject BuildGameObject(Tile tile, Rule rule, Model model, List<MapPoint> points,
            float elevation, float minHeight)
        {
            var gameObjectWrapper = GameObjectFactory
                .CreateNew(GetName(model), tile.GameObject);

            // NOTE observed that min_height should be subracted from height for building:part
            // TODO this should be done in mapcss, but stylesheet doesn't support multiply eval operations
            // on the same tag

            var height = rule.GetHeight();
            if (rule.IsPart())
                height -= minHeight;

            // TODO should we save this object in WorldManager?
            var building = new Building
            {
                Id = model.Id,
                Address = AddressExtractor.Extract(model.Tags),
                GameObject = gameObjectWrapper,
                Height = height,
                Levels = rule.GetLevels(),
                MinHeight = minHeight,
                Type = rule.GetFacadeBuilder(),
                FacadeType = rule.GetFacadeBuilder(),
                FacadeColor = rule.GetFacadeColor(),
                FacadeMaterial = rule.GetFacadeMaterial(),
                RoofType = rule.GetRoofBuilder(),
                RoofColor = rule.GetRoofColor(),
                RoofMaterial = rule.GetRoofMaterial(),
                RoofHeight = rule.GetRoofHeight(),
                Elevation = elevation, // we set equal elevation for every point
                Footprint = points,
            };

            var facadeBuilder = _facadeBuilders.Single(f => f.Name == building.FacadeType);
            var roofBuilder = _roofBuilders.Single(f => f.Name == building.RoofType);

            var facadeMeshData = facadeBuilder.Build(building);
            var roofMeshData = roofBuilder.Build(building);

            Scheduler.MainThread.Schedule(() =>
            {
                // NOTE use different gameObject only to support different materials
                AttachChildGameObject(building.GameObject, "facade", facadeMeshData);
                AttachChildGameObject(building.GameObject, "roof", roofMeshData);
            });

            tile.Registry.RegisterGlobal(building.Id);

            return gameObjectWrapper;
        }

        /// <summary> Process unity's game object. </summary>
        protected virtual void AttachChildGameObject(IGameObject parent, string name, MeshData meshData)
        {
            GameObject gameObject = GetGameObject(meshData);
            gameObject.isStatic = true;
            gameObject.transform.parent = parent.GetComponent<GameObject>().transform;
            gameObject.name = name;
            gameObject.renderer.sharedMaterial = ResourceProvider
              .GetMatertial(meshData.MaterialKey);
        }

        private GameObject GetGameObject(MeshData meshData)
        {
            // GameObject was created directly in builder, so we can use it and ignore other meshData properties.
            // also we expect that all components are defined
            if (meshData.GameObject != null && !meshData.GameObject.IsEmpty)
                return meshData.GameObject.GetComponent<GameObject>();

            var gameObject = new GameObject();
            var mesh = new Mesh();
            mesh.vertices = meshData.Vertices.ToArray();
            mesh.triangles = meshData.Triangles.ToArray();
            mesh.colors = meshData.Colors.ToArray();

            mesh.RecalculateNormals();

            ObjectPool.RecycleMeshData(meshData);

            gameObject.AddComponent<MeshFilter>().mesh = mesh;
            gameObject.AddComponent<MeshCollider>();
            gameObject.AddComponent<MeshRenderer>();

            return gameObject;
        }
    }
}