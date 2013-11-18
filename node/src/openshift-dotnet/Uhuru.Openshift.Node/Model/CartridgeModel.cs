using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime
{
    public class CartridgeModel
    {
        public ApplicationContainer Container{get;set;}

        public CartridgeModel(ApplicationContainer container)
        {
            Container = container;
        }

        public string Configure(string cartName, string templateGitUrl, string manifest)
        {
            return PopulateGearRepo(cartName, templateGitUrl);
        }

        private string PopulateGearRepo(string cartName, string templateGitUrl)
        {
            ApplicationRepository repo = new ApplicationRepository(Container);
            repo.PopulateFromCartridge(cartName);
            return string.Empty;
        }
    }
}
