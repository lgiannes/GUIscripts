namespace GPIO
{
    Class Interface
    {
        public Interface(){}

        void TurnOnFEB()
        {    
            BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
            BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
            //Sync.Sleep(50);
            BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
            Sync.Sleep(1500);
        }
        
        void TurnOffFEB()
        {    
            BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
            BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
            //Sync.Sleep(50);
            BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
            Sync.Sleep(3000);
        }
    }


}