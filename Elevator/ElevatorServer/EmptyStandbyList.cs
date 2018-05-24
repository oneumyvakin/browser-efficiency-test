namespace Elevator
{
    internal class EmptyStandbyList
    {
        private bool Enabled;
        const string BinaryPath = "EmptyStandbyList.exe";
        
        public EmptyStandbyList(bool emptyStandbyListEnabled)
        {
            Enabled = emptyStandbyListEnabled;
        }

        /// <summary>
        /// Start EmptyStandbyList.exe.
        /// </summary>
        /// <returns>True if completed with status 0.</returns>
        public bool Start()
        {
            if (!Enabled)
            {
                return false;
            }

            var args = new string[] { "workingsets", "modifiedpagelist", "standbylist", "priority0standbylist" };            
            
            foreach(var arg in args)
            {
                if (!RunBinary.Run(BinaryPath, arg))
                {
                    return false;
                }
            }            

            return true;
        }        
    }
}
