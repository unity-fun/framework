using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mercraft.Infrastructure.Config;
using Mercraft.Infrastructure.Dependencies;

namespace Mercraft.Core.Elevation.Srtm
{
    public class SrtmElevationProvider : IElevationProvider, IConfigurable
	{
        private const string PathKey = "";

        private string _dataDirectory;
	    private List<SrtmDataCell> _dataCells;

        private readonly IPathResolver _pathResolver;

        [Dependency]
        public SrtmElevationProvider(IPathResolver pathResolver)
        {
            _pathResolver = pathResolver;
			_dataCells = new List<SrtmDataCell> ();
		}

        public float GetElevation(double latitude, double longitude)
	    {
            int cellLatitude = (int)Math.Floor(Math.Abs(latitude));
            if (latitude < 0)
                cellLatitude *= -1;

            int cellLongitude = (int)Math.Floor(Math.Abs(longitude));
            if (longitude < 0)
                cellLongitude *= -1;

            SrtmDataCell dataCell = _dataCells
                .FirstOrDefault(dc => dc.Latitude == cellLatitude && dc.Longitude == cellLongitude);
            if (dataCell != null)
                return dataCell.GetElevation(latitude, longitude);

            string filename = string.Format("{0}{1:D2}{2}{3:D3}.hgt",
                cellLatitude < 0 ? "S" : "N",
                Math.Abs(cellLatitude),
                cellLongitude < 0 ? "W" : "E",
                Math.Abs(cellLongitude));

            string filePath = Path.Combine(_dataDirectory, filename);

            if (!File.Exists(filePath))
                throw new Exception("SRTM data cell not found: " + filePath);

            dataCell = new SrtmDataCell(filePath);
            _dataCells.Add(dataCell);
            return dataCell.GetElevation(latitude, longitude);
	    }

	    public float GetElevation(GeoCoordinate geoCoordinate)
	    {
	        return GetElevation(geoCoordinate.Latitude, geoCoordinate.Longitude);
	    }

	    public void Configure(IConfigSection configSection)
	    {
            var path = configSection.GetString(PathKey);
	        _dataDirectory = _pathResolver.Resolve(path);
	    }
	}
}
