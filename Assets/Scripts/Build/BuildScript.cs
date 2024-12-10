using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Build
{
    public class BuildScript: MonoBehaviour
    {
        private const string UWPSDKVersion = "10.0.19041.0";

#if UNITY_EDITOR
        static void Build()
        {
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClient, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClientServer, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.PrivateNetworkClientServer, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.SpatialPerception, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.Microphone, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.GazeInput, true);
        
            //EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.WSAPlayer);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WSA, BuildTarget.WSAPlayer);
            EditorUserBuildSettings.wsaUWPSDK = UWPSDKVersion;
            EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.D3D;
            EditorUserBuildSettings.wsaArchitecture = "ARM64";
            EditorUserBuildSettings.wsaSubtarget = WSASubtarget.AnyDevice;
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WSA, Il2CppCompilerConfiguration.Release);

            BuildPipeline.BuildPlayer(GetScenes(), "app", BuildTarget.WSAPlayer, BuildOptions.None);
            //UpdatePackageManifest("app/");
        }

        private static String[] GetScenes()
        {
            return EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        }
        
            private static bool UpdatePackageManifest(string locationPathName)
            {
                var buildDir = Path.Combine(locationPathName, string.Concat(PlayerSettings.productName, "\\"));
                // Find the manifest, assume the one we want is the first one
                string[] manifests = Directory.GetFiles(buildDir, "Package.appxmanifest", SearchOption.AllDirectories);
        
                if (manifests.Length == 0)
                {
                    Debug.LogError(string.Format("Unable to find Package.appxmanifest file for build (in path - {0})", buildDir));
                    return false;
                }
        
                string manifest = manifests[0];
                var rootNode = XElement.Load(manifest);
                var identityNode = rootNode.Element(rootNode.GetDefaultNamespace() + "Identity");
        
                if (identityNode == null)
                {
                    Debug.LogError(string.Format("Package.appxmanifest for build (in path - {0}) is missing an <Identity /> node", buildDir));
                    return false;
                }
        
                var versionAttr = identityNode.Attribute(XName.Get("Version"));
        
                if (versionAttr == null)
                {
                    Debug.LogError(string.Format("Package.appxmanifest for build (in path - {0}) is missing a version attribute in the <Identity /> node.", buildDir));
                    return false;
                }
        
                // preparing and updating new package Version
                var version = PlayerSettings.WSA.packageVersion;
                var now = DateTime.Now;
                //Attention: this method only works until the year 2099 ;)
                var newVersion = new Version(now.Year - 2000, now.Month * 100 + now.Day, now.Hour, now.Minute * 100 + now.Second);
        
                PlayerSettings.WSA.packageVersion = newVersion;
                versionAttr.Value = newVersion.ToString();
        
                var deps = rootNode.Element(rootNode.GetDefaultNamespace() + "Dependencies");
                var devFam = deps.Element(rootNode.GetDefaultNamespace() + "TargetDeviceFamily");
                devFam.Attribute(XName.Get("MinVersion")).Value = UWPSDKVersion; //set min WinSDK Version to desired
                devFam.Attribute(XName.Get("MaxVersionTested")).Value = UWPSDKVersion;//same for max WinSDK verison
                rootNode.Save(manifest);
                return true;
            }
#endif
    }
}