    
void ScriptMain(){    

    string GPIO_calib_file = "/DATA/neutrino/FCT/GPIO_cal/GPIO_SN47/cal_GPIO_SN47.csv";
    var reader = new System.IO.StreamReader(@GPIO_calib_file);
    int i=0;
    // System.Console.WriteLine("read csv file");


    double[] GPIOgain = new double[8];
    double[] GPIOoffset = new double[8];
    double[] GPIORmax = new double[8];
    double[] GPIORmin = new double[8];

    while (!reader.EndOfStream){
            var line = reader.ReadLine();
            var values = line.Split(';');
            // System.Console.WriteLine(reader.EndOfStream);
            // System.Console.WriteLine(i);
            // System.Console.WriteLine(values[0]);

            if(i>0){
            // System.Console.WriteLine("filling.."+i);
                GPIOgain[i-1]=Convert.ToDouble(values[0]);
                GPIOoffset[i-1]=Convert.ToDouble(values[1]);
                GPIORmax[i-1]=Convert.ToDouble(values[2]);
                GPIORmin[i-1]=Convert.ToDouble(values[3]);
            }
            i++;

    }
            // System.Console.WriteLine(" ");


    // for(int ch=0;ch<8;ch++){
    //     System.Console.WriteLine(
    //          GPIOgain[ch]
    //     +" "+GPIOoffset[ch]
    //     +" "+GPIORmax[ch]
    //     +" "+GPIORmin[ch]);
    // }
}