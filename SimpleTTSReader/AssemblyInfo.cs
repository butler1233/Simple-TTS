﻿#region

using System;
using System.Deployment.Application;
using System.Reflection;

#endregion

namespace SimpleTTSReader
{
    internal class AssemblyInfo
    {
        public static string GetAssemblyAttribute<T>(Func<T, string> value)
            where T : Attribute
        {
            var attribute = (T) Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof (T));
            return value.Invoke(attribute);
        }

        public static string GetVersionString()
        {
            var obj = ApplicationDeployment.IsNetworkDeployed
                ? ApplicationDeployment.CurrentDeployment.
                    CurrentVersion
                : Assembly.GetExecutingAssembly().GetName().Version;
            return $"{obj.Major}.{obj.Minor}.{obj.Build}";
        }

        public static string GetVersionString(Version version)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        public static Version GetVersion()
        {
            var obj = ApplicationDeployment.IsNetworkDeployed
                ? ApplicationDeployment.CurrentDeployment.
                    CurrentVersion
                : Assembly.GetExecutingAssembly().GetName().Version;
            return obj;
        }

        public static string GetCopyright()
        {
            return GetAssemblyAttribute<AssemblyCopyrightAttribute>(a => a.Copyright);
        }

        public static string GetTitle()
        {
            return GetAssemblyAttribute<AssemblyTitleAttribute>(a => a.Title);
        }
    }
}