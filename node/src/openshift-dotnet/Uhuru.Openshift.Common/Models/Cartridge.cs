using System;
using System.Collections.Generic;

namespace Uhuru.Openshift.Common.Models
{
    public class Cartridge
    {
        public string OriginalName { get; set; }

        public string Version { get; set; }

        public List<object> Versions { get; set; }

        public string Architecture { get; set; }

        public string DisplayName { get; set; }

        public string License { get; set; }

        public string LicenseUrl { get; set; }

        public string Vendor { get; set; }

        public string CartridgeVendor { get; set; }

        public string Description { get; set; }

        public List<object> Provides { get; set; }

        public List<object> Requires { get; set; }

        public List<object> Conflicts { get; set; }

        public List<object> NativeRequires { get; set; }

        public List<object> Categories { get; set; }

        public string Website { get; set; }

        public List<object> Suggests { get; set; }

        public dynamic HelpTopics { get; set; }

        public dynamic CartDataDef { get; set; }

        public List<object> AdditionalControlActions { get; set; }

        public List<Endpoint> Endpoints { get; set; }

        public List<Profile> Profiles { get; set; }

        public string DefaultProfile { get; set; }

        public dynamic Spec { get; set; }

        public Dictionary<string, Profile> ProfileMap { get; set; }

        public string Platform { get; set; }


        public Cartridge()
        {
            ProfileMap = new Dictionary<string, Profile>();
            Profiles = new List<Profile>();
            Endpoints = new List<Endpoint>();
        }

        public static Cartridge FromDescriptor(dynamic spec)
        {
            Cartridge cart = new Cartridge();

            cart.Spec = spec;
            cart.OriginalName = spec["Name"];
            cart.Version = spec.ContainsKey("Version") ? spec["Version"] : "0.0";
            cart.Versions = spec.ContainsKey("Versions") ? spec["Versions"] : new List<object>();
            cart.Architecture = spec.ContainsKey("Architecture") ? spec["Architecture"] : "noarch";
            cart.DisplayName = spec.ContainsKey("Display-Name") ? spec["Display-Name"] : string.Format("{0}-{1}-{2}", cart.OriginalName, cart.Version, cart.Architecture);
            cart.License = spec["License"] ?? "unknown";
            cart.LicenseUrl = spec["License-Url"] ?? "";
            cart.Vendor = spec["Vendor"] ?? "unknown";
            cart.CartridgeVendor = spec["Cartridge-Vendor"] ?? "unknown";
            cart.Description = spec["Description"] ?? "";
            cart.Platform = spec["Platform"] ?? "Windows";

            if (spec.ContainsKey("Provides"))
            {
                if (spec["Provides"] is String)
                {
                    cart.Provides = new List<object>() { spec["Provides"] };
                }
                else
                {
                    cart.Provides = spec["Provides"];
                }
            }
            else
            {
                cart.Provides = new List<object>() { };
            }
            if (spec.ContainsKey("Requires"))
            {
                if (spec["Requires"] is String)
                {
                    cart.Requires = new List<object>() { spec["Requires"] };
                }
                else
                {
                    cart.Requires = spec["Requires"];
                }
            }
            else
            {
                cart.Requires = new List<object>() { };
            }
            if (spec.ContainsKey("Conflicts"))
            {
                if (spec["Conflicts"] is String)
                {
                    cart.Conflicts = new List<object>() { spec["Conflicts"] };
                }
                else
                {
                    cart.Conflicts = spec["Conflicts"];
                }
            }
            else
            {
                cart.Conflicts = new List<object>() { };
            }
            if (spec.ContainsKey("Native-Requires"))
            {
                if (spec["Native-Requires"] is String)
                {
                    cart.NativeRequires = new List<object>() { spec["Native-Requires"] };
                }
                else
                {
                    cart.NativeRequires = spec["Native-Requires"];
                }
            }
            else
            {
                cart.NativeRequires = new List<object>() { };
            }
            cart.Categories = spec["Categories"] ?? new List<object>() { };
            cart.Website = spec["Website"] ?? "";
            cart.HelpTopics = spec["Help-Topics"] ?? new object();
            cart.CartDataDef = spec["Cart-Data"] ?? new object();
            cart.AdditionalControlActions = spec.ContainsKey("Additional-Control-Actions") ? spec["Additional-Control-Actions"] : new List<object>() { };

            if (((Dictionary<object, object>)spec).ContainsKey("Endpoints"))
            {
                if (spec["Endpoints"] is List<object>)
                {
                    foreach (dynamic ep in spec["Endpoints"])
                    {
                        cart.Endpoints.Add(Endpoint.FromDescriptor(ep, string.Empty));
                    }
                }
            }

            if (((Dictionary<object, object>)spec).ContainsKey("Profiles"))
            {
                foreach (dynamic p in spec["Profiles"])
                {
                    KeyValuePair<string, dynamic> pair = (KeyValuePair<string, dynamic>)p;
                    Profile profile = Profile.FromDescriptor(cart, pair.Value);
                    profile.Name = pair.Key;
                    cart.Profiles.Add(profile);
                    cart.ProfileMap[pair.Key] = profile;
                }
            }
            else
            {
                Profile profile = Profile.FromDescriptor(cart, spec);
                profile.Name = cart.Name;
                profile.Generated = true;
                cart.Profiles.Add(profile);
                cart.ProfileMap[profile.Name] = profile;
            }
            cart.DefaultProfile = spec.ContainsKey("Default-Profile") ? spec["Default-Profile"] : cart.Profiles[0].Name;

            return cart;
        }

        public dynamic ToDescriptor()
        {
            Dictionary<object, object> h = new Dictionary<object, object>();
            h["Name"] = this.OriginalName;
            h["Display-Name"] = this.DisplayName;
            h["Architecture"] = this.Architecture;
            h["Version"] = this.Version;
            h["Versions"] = this.Versions;
            h["Description"] = this.Description;
            h["License-Url"] = this.LicenseUrl;
            h["Categories"] = this.Categories;
            h["Website"] = this.Website;
            h["HelpTopics"] = this.HelpTopics;
            h["Cart-Data"] = this.CartDataDef;
            h["Platform"] = this.Platform;

            if (this.AdditionalControlActions.Count > 0)
                h["Additional-Control-Actions"] = this.AdditionalControlActions;
            if (this.Provides.Count > 0)
                h["Provides"] = this.Provides;
            if (this.Requires.Count > 0)
                h["Requires"] = this.Requires;
            if (this.Conflicts.Count > 0)
                h["Conflicts"] = this.Conflicts;
            if (this.Suggests != null && this.Suggests.Count > 0)
                h["Suggests"] = this.Suggests;
            if (this.NativeRequires.Count > 0)
                h["Native-Requires"] = this.NativeRequires;
            h["Vendor"] = this.Vendor;
            h["Cartridge-Vendor"] = this.CartridgeVendor;
            if (this.DefaultProfile != null && !this.ProfileMap[this.DefaultProfile].Generated)
                h["Default-Profile"] = this.DefaultProfile;

            if (this.Endpoints.Count > 0)
            {
                List<object> endps = new List<object>();
                foreach (Endpoint ep in this.Endpoints)
                {
                    endps.Add(ep.ToDescriptor());
                }
                h["Endpoints"] = endps;
            }

            if (this.Profiles.Count == 1 && this.Profiles[0].Generated)
            {
                dynamic profile = this.Profiles[0].ToDescriptor();
                profile.Remove("Name");
                foreach (KeyValuePair<object, object> pair in profile)
                {
                    h[pair.Key] = profile[pair.Key];
                }
            }
            else
            {
                Dictionary<object, object> profiles = new Dictionary<object, object>();
                foreach (Profile prof in this.Profiles)
                {
                    profiles[prof.Name] = prof.ToDescriptor();
                }
                h["Profiles"] = profiles;
            }

            return h;
        }

        public string Name
        {
            get
            {
                if (this.CartridgeVendor == "redhat" || this.CartridgeVendor == "uhuru" || string.IsNullOrEmpty(this.CartridgeVendor))
                {
                    return string.Format("{0}-{1}", this.OriginalName, this.Version);
                }
                else
                {
                    return string.Format("{0}-{1}-{2}", this.CartridgeVendor, this.OriginalName, this.Version);
                }
            }
        }

    }
}