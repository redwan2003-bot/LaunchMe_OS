namespace LaunchMe.Core.Wallpapers;

/// <summary>
/// Solar position, sunrise/sunset, and time-of-day calculator using standard NOAA astronomical formulas.
/// </summary>
public static class SunTimeCalculator
{
    private const double Rad2Deg = 180.0 / Math.PI;
    private const double Deg2Rad = Math.PI / 180.0;
    private const double ZenithSunriseSunset = 90.833; // Standard atmospheric refraction at horizon

    /// <summary>
    /// Calculates solar coordinates, sunrise, sunset, and daylight phase for a given timestamp and coordinates.
    /// Default coordinates: San Francisco / Cupertino (37.7749° N, -122.4194° W).
    /// </summary>
    public static SolarInfo CalculateSolarInfo(
        DateTimeOffset timestamp,
        double latitude = 37.7749,
        double longitude = -122.4194)
    {
        var utc = timestamp.ToUniversalTime();
        var dayOfYear = utc.DayOfYear;
        var totalDaysInYear = DateTime.IsLeapYear(utc.Year) ? 366.0 : 365.0;

        // Fractional year in radians
        var gamma = 2.0 * Math.PI / totalDaysInYear * (dayOfYear - 1 + (utc.Hour - 12.0) / 24.0);

        // Equation of time in minutes
        var eqTimeMinutes = 229.18 * (0.000075
            + 0.001868 * Math.Cos(gamma)
            - 0.032077 * Math.Sin(gamma)
            - 0.014615 * Math.Cos(2.0 * gamma)
            - 0.040849 * Math.Sin(2.0 * gamma));

        // Solar declination in radians
        var declination = 0.006918
            - 0.399912 * Math.Cos(gamma)
            + 0.070257 * Math.Sin(gamma)
            - 0.006758 * Math.Cos(2.0 * gamma)
            + 0.000907 * Math.Sin(2.0 * gamma)
            - 0.002697 * Math.Cos(3.0 * gamma)
            + 0.001480 * Math.Sin(3.0 * gamma);

        var latRad = latitude * Deg2Rad;

        // Solar Noon in minutes from midnight UTC
        var solarNoonUtcMinutes = 720.0 - (4.0 * longitude) - eqTimeMinutes;
        var solarNoonUtc = TimeSpan.FromMinutes(ClampMinutes(solarNoonUtcMinutes));

        // Hour angle for sunrise / sunset
        var cosOmega0 = (Math.Cos(ZenithSunriseSunset * Deg2Rad) - Math.Sin(latRad) * Math.Sin(declination))
                        / (Math.Cos(latRad) * Math.Cos(declination));

        TimeSpan sunriseUtc;
        TimeSpan sunsetUtc;

        if (cosOmega0 > 1.0)
        {
            // Polar night: Sun never rises
            sunriseUtc = TimeSpan.FromHours(12);
            sunsetUtc = TimeSpan.FromHours(12);
        }
        else if (cosOmega0 < -1.0)
        {
            // Polar day / Midnight sun: Sun never sets
            sunriseUtc = TimeSpan.FromHours(0);
            sunsetUtc = TimeSpan.FromHours(23.999);
        }
        else
        {
            var hourAngleDeg = Math.Acos(cosOmega0) * Rad2Deg;
            var sunriseMinutes = solarNoonUtcMinutes - (4.0 * hourAngleDeg);
            var sunsetMinutes = solarNoonUtcMinutes + (4.0 * hourAngleDeg);

            sunriseUtc = TimeSpan.FromMinutes(ClampMinutes(sunriseMinutes));
            sunsetUtc = TimeSpan.FromMinutes(ClampMinutes(sunsetMinutes));
        }

        // Current solar position
        var timeOffsetMinutes = utc.TimeOfDay.TotalMinutes;
        var trueSolarTime = timeOffsetMinutes + (4.0 * longitude) + eqTimeMinutes;
        var hourAngle = (trueSolarTime / 4.0) - 180.0;
        if (hourAngle < -180.0) hourAngle += 360.0;
        if (hourAngle > 180.0) hourAngle -= 360.0;

        var hourAngleRad = hourAngle * Deg2Rad;

        // Elevation
        var sinElevation = Math.Sin(latRad) * Math.Sin(declination)
                         + Math.Cos(latRad) * Math.Cos(declination) * Math.Cos(hourAngleRad);
        sinElevation = Math.Clamp(sinElevation, -1.0, 1.0);
        var elevation = Math.Asin(sinElevation) * Rad2Deg;

        // Azimuth
        var cosZenith = sinElevation;
        var sinZenith = Math.Sqrt(Math.Max(0.0, 1.0 - cosZenith * cosZenith));
        var cosAzimuth = (Math.Sin(declination) - Math.Sin(latRad) * cosZenith)
                       / (Math.Cos(latRad) * (sinZenith > 1e-6 ? sinZenith : 1e-6));
        cosAzimuth = Math.Clamp(cosAzimuth, -1.0, 1.0);
        var azimuth = Math.Acos(cosAzimuth) * Rad2Deg;
        if (hourAngle > 0.0)
        {
            azimuth = 360.0 - azimuth;
        }

        var isDaylight = elevation > -0.833;
        var isMorning = hourAngle < 0.0;
        var phase = DetermineSunPhase(elevation, isMorning);

        // Convert UTC sunrise/sunset to local offset time of day
        var localOffset = timestamp.Offset;
        var sunriseLocal = TimeSpan.FromMinutes(ClampMinutes((sunriseUtc + localOffset).TotalMinutes));
        var sunsetLocal = TimeSpan.FromMinutes(ClampMinutes((sunsetUtc + localOffset).TotalMinutes));
        var solarNoonLocal = TimeSpan.FromMinutes(ClampMinutes((solarNoonUtc + localOffset).TotalMinutes));

        return new SolarInfo
        {
            Timestamp = timestamp,
            ElevationDegrees = Math.Round(elevation, 2),
            AzimuthDegrees = Math.Round(azimuth, 2),
            IsDaylight = isDaylight,
            Phase = phase,
            SunriseTime = sunriseLocal,
            SunsetTime = sunsetLocal,
            SolarNoonTime = solarNoonLocal
        };
    }

    /// <summary>
    /// Maps solar position to a dynamic wallpaper slice index (e.g. 0 to totalSlices - 1).
    /// </summary>
    public static int GetDynamicWallpaperSliceIndex(
        DateTimeOffset timestamp,
        int totalSlices = 16,
        double latitude = 37.7749,
        double longitude = -122.4194)
    {
        if (totalSlices <= 1) return 0;

        var info = CalculateSolarInfo(timestamp, latitude, longitude);
        var localTimeMinutes = timestamp.TimeOfDay.TotalMinutes;
        var sunriseMinutes = info.SunriseTime.TotalMinutes;
        var sunsetMinutes = info.SunsetTime.TotalMinutes;

        if (sunsetMinutes <= sunriseMinutes)
        {
            sunsetMinutes += 1440.0;
            if (localTimeMinutes < sunriseMinutes)
            {
                localTimeMinutes += 1440.0;
            }
        }

        var dayDuration = sunsetMinutes - sunriseMinutes;
        var nightDuration = 1440.0 - dayDuration;

        // Allocate half the slices to day and half to night
        var daySlices = totalSlices / 2;
        var nightSlices = totalSlices - daySlices;

        if (localTimeMinutes >= sunriseMinutes && localTimeMinutes <= sunsetMinutes)
        {
            // Daytime progression
            var dayProgress = (localTimeMinutes - sunriseMinutes) / dayDuration;
            var slice = (int)(dayProgress * daySlices);
            return Math.Clamp(slice, 0, daySlices - 1);
        }
        else
        {
            // Nighttime progression
            double nightProgress;
            if (localTimeMinutes > sunsetMinutes)
            {
                nightProgress = (localTimeMinutes - sunsetMinutes) / nightDuration;
            }
            else
            {
                nightProgress = (localTimeMinutes + 1440.0 - sunsetMinutes) / nightDuration;
            }

            var slice = daySlices + (int)(nightProgress * nightSlices);
            return Math.Clamp(slice, daySlices, totalSlices - 1);
        }
    }

    private static SunPhase DetermineSunPhase(double elevation, bool isMorning)
    {
        if (elevation < -18.0) return SunPhase.AstronomicalNight;
        if (elevation < -12.0) return SunPhase.NauticalTwilight;
        if (elevation < -6.0) return isMorning ? SunPhase.Dawn : SunPhase.Dusk;
        if (elevation < 0.0) return isMorning ? SunPhase.CivilTwilight : SunPhase.CivilTwilight;
        if (elevation < 6.0) return isMorning ? SunPhase.Sunrise : SunPhase.Sunset;
        if (elevation < 15.0 && !isMorning) return SunPhase.GoldenHour;
        if (elevation > 50.0) return SunPhase.SolarNoon;
        return isMorning ? SunPhase.Morning : SunPhase.Afternoon;
    }

    private static double ClampMinutes(double minutes)
    {
        while (minutes < 0.0) minutes += 1440.0;
        while (minutes >= 1440.0) minutes -= 1440.0;
        return minutes;
    }
}
