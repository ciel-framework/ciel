using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Task = Microsoft.Build.Utilities.Task;

namespace Ciel.BuildTasks
{
    public class SimpleTask : Task
    {
        public override bool Execute()
        {
            return true;
        }
    }
}