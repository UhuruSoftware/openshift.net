using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime
{
    public enum State
    {
        BUILDING,
        DEPLOYING,
        IDLE,
        NEW,
        STARTED,
        STOPPED,
        UNKNOWN
    }
}

namespace Uhuru.Openshift.Runtime.Utils
{    
    public class ApplicationState
    {
        public string Uuid { get; set; }

        private string stateFile;
        private ApplicationContainer container;        

        public ApplicationState(ApplicationContainer container)
        {
            this.container = container;
            this.Uuid = container.Uuid;
            this.stateFile = Path.Combine(this.container.ContainerDir, "app-root", "runtime", ".state");
        }

        public ApplicationState Value(State newState)
        {
            File.WriteAllText(stateFile, newState.ToString());
            this.container.SetRWPermissions(this.stateFile);
            return this;
        }

        public State Value()
        {
            string state = File.ReadAllText(this.stateFile);
            if (Enum.IsDefined(typeof(State), state))
            {
                return (State)Enum.Parse(typeof(State), state);
            }
            return State.UNKNOWN;
        }
    }
}
