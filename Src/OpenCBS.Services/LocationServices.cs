// Octopus MFS is an integrated suite for managing a Micro Finance Institution: 
// clients, contracts, accounting, reporting and risk
// Copyright © 2006,2007 OCTO Technology & OXUS Development Network
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
//
// Website: http://www.opencbs.com
// Contact: contact@opencbs.com

using System;
using System.Collections.Generic;
using OpenCBS.CoreDomain;
using OpenCBS.ExceptionsHandler;
using OpenCBS.Manager;


namespace OpenCBS.Services
{
    public class LocationServices : MarshalByRefObject
    {
        private readonly LocationsManager _locationsManager;

        public LocationServices(User pUser)
        {
            _locationsManager = new LocationsManager(pUser);
        }

        public LocationServices(LocationsManager pLocationsManager)
        {
            _locationsManager = pLocationsManager;
        }

        public List<Province> GetProvinces()
        {
            return _locationsManager.GetProvinces();
        }

        public List<District> GetDistricts()
        {
            return _locationsManager.GetDistricts();
        }

        public List<City> GetCities()
        {
            return _locationsManager.GetCities();
        }

        public int AddProvince(string name)
        {
            CheckLocation(name);
            return _locationsManager.AddProvince(name);
        }

        private static void CheckLocation(string name)
        {
            if (name == null || name.Trim(' ') == string.Empty)
                throw new OpenCbsEmptyLocationException();
        }

        public bool UpdateProvince(Province province)
        {
            CheckLocation(province.Name);
            return _locationsManager.UpdateProvince(province);
        }

        public bool DeleteProvince(Province pProvince)
        {
            bool deleteProvincePossible = true;
            List<District> districts = new List<District>();
            districts = _locationsManager.GetDistricts();
            for (int i = 0; i < districts.Count; i++)
            {
                if (districts[i].Province.Id != pProvince.Id)
                    continue;
                deleteProvincePossible = false;
                break;
            }
            if (deleteProvincePossible)
                _locationsManager.DeleteProvinceById(pProvince.Id);
            return deleteProvincePossible;
        }

        public int AddDistrict(string name, int pProvinceId)
        {
            CheckLocation(name);
            return _locationsManager.AddDistrict(name, pProvinceId);
        }

        public bool UpdateDistrict(District district)
        {
            CheckLocation(district.Name);
            return _locationsManager.UpdateDistrict(district);
        }

        public bool DeleteDistrict(int districtId)
        {
            bool deleteDistrictPossible = true;
            List<City> cities = new List<City>();
            cities = _locationsManager.GetCities();
            for (int i = 0; i < cities.Count; i++)
            {
                if (cities[i].DistrictId != districtId)
                    continue;
                deleteDistrictPossible = false;
                break;
            }
            CheckDistrictToDelete(districtId);
            if (deleteDistrictPossible)
                _locationsManager.DeleteDistrictById(districtId);
            return deleteDistrictPossible;
        }

        private void CheckDistrictToDelete(int districtId)
        {
            if (_locationsManager.IsDistrictUsed(districtId))
                throw new OpenCbsDistrictUsedException();
        }

        public int AddCity(City city)
        {
            CheckLocation(city.Name);
            return _locationsManager.AddCity(city);
        }

        public void DeleteCity(int cityId)
        {
            CheckCityToDelete(cityId);
            _locationsManager.DeleteCityById(cityId);
        }

        private void CheckCityToDelete(int cityId)
        {
            if (_locationsManager.IsCityUsed(cityId))
                throw new OpenCbsCityUsedException();
        }

        public bool UpdateCity(City city)
        {
            CheckLocation(city.Name);
            return _locationsManager.UpdateCity(city);
        }

        public List<Province> FindAllProvinces()
        {
            return _locationsManager.SelectAllProvinces();
        }

        public List<City> FindCityByDistrictId(int districtId)
        {
            return _locationsManager.SelectCityByDistrictId(districtId);
        }

        public List<City> FindCitiesByDistrictOrProvince(District district, Province province)
        {
            List<City> cities;
            if (district != null)
                cities = _locationsManager.SelectCityByDistrictId(district.Id);
            else if (province.Id != 0)
            {
                var districts = _locationsManager.SelectDistrictsByProvinceId(province.Id);
                cities = new List<City>();
                foreach (District dis in districts)
                    cities.AddRange(
                        _locationsManager.SelectCityByDistrictId(dis.Id));
            }
            else
            {
                cities = _locationsManager.GetCities();
            }
            return cities;
        }

        public List<District> FindDistrict(Province province)
        {
            return province.Id == 0 ? _locationsManager.GetDistricts() : _locationsManager.SelectDistrictsByProvinceId(province.Id);
        }

        public Province FindProvinceByName(string name)
        {
            return _locationsManager.SelectProvinceByName(name);
        }

        public District FindDistirctByName(string name)
        {
            return _locationsManager.SelectDistrictByName(name);
        }

        public District FindDistrictByCityName(string name)
        {
            return _locationsManager.SelectDistrictByCityName(name);
        }
    }
}
