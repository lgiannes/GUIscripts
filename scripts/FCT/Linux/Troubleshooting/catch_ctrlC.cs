    
void ScriptMain(){    
    System.Console.CancelKeyPress += delegate {
        System.Console.WriteLine("you pressed ctrl-C!");
    };

    while (true) {
        System.Console.WriteLine("...");
        Sync.Sleep(1000);
    }

}