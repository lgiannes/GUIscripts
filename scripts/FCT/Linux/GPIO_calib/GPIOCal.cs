void ScriptMain()
{	
	
	Console.WriteLine("Script begins...");
	
	string l_basefile = "/home/neutrino/FCT/GPIO_cal/cal_all";
	string[] l_voltages = new string[2]{"5V", "55V"};
	double[] l_dmm = new double[2];
	double[][] l_mean = new double[2][];
	
	BoardLib.SetBoardId(126);
	// init ADC 
	BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart", true);
	BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
	
	var l_strBCal = new  System.Text.StringBuilder(); // for Calibration
	
	//-------------------------------------------------
	// Measurements
	//-------------------------------------------------
	for (int l_i=0; l_i<l_voltages.Length; l_i++)
	{
		Dialog.ShowDialog("Apply " + l_voltages[l_i] + " on HV power supply and record DMM measurement");
		l_dmm[l_i] = Dialog.ShowInputDialog<double>("DMM measurement for " + l_voltages[l_i] + " ?");
		l_mean[l_i] = ExtractAdcData(100, l_basefile + "_" + l_voltages[l_i], true, l_strBCal);
		l_strBCal.AppendLine();
	}	
		
	Console.WriteLine("\nExtracted ADC Data");
	
	//-------------------------------------------------
	// Calibration parameters
	//-------------------------------------------------
	CalibrationParam(l_mean, l_dmm, l_strBCal);
	
	//-------------------------------------------------
	// Print report to Calibration CSV
	//-------------------------------------------------
	try
	{
		System.IO.File.WriteAllText(l_basefile + "_Cal.csv" , l_strBCal.ToString());
	}
	catch
	{
		Console.WriteLine("ERROR : IO access during CSV write");
	}
	
	// We're done!
	Console.WriteLine("\nScript ended");
}


// ---------------------------------------------------------------------------
// Compute calibration parameters
// input: 
//		x_mean:		array of mean values measured
//		x_dmm_ref:	dmm reference voltage
//		x_strBCal: 	string builder for calibration results
// ---------------------------------------------------------------------------
void CalibrationParam(double[][] x_mean, double[] x_dmm_ref, System.Text.StringBuilder x_strBCal)
{
	const string l_sep = ";";
	const int l_nbVar = 8;
	
	// write measurements
	string l_row = "DMMmin (V)" + l_sep;
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_row += x_dmm_ref[0].ToString("F5") + l_sep;
	}
	x_strBCal.AppendLine(l_row);
	
	l_row = "DMMmax (V)" + l_sep;
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_row += x_dmm_ref[1].ToString("F5") + l_sep;
	}
	x_strBCal.AppendLine(l_row);
	
	l_row = "ADCmin (LSB)" + l_sep;
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_row += x_mean[0][l_i].ToString("F5") + l_sep;
	}
	x_strBCal.AppendLine(l_row);
	
	l_row = "ADCmax (LSB)" + l_sep;
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_row += x_mean[1][l_i].ToString("F5") + l_sep;
	}
	x_strBCal.AppendLine(l_row);
	
	// compute floating coefs.
	double[] l_gain_f = new double[l_nbVar];
	double[] l_offset_f = new double[l_nbVar];
	double l_a = (1.0+1000.0/29.4)*3.0/262144.0;
	double l_b = 0;
	
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_gain_f[l_i] = (x_dmm_ref[1] - x_dmm_ref[0])/((x_mean[1][l_i] - x_mean[0][l_i])*l_a);
		l_offset_f[l_i] = (x_dmm_ref[0]-l_b)/(l_gain_f[l_i]*l_a) - x_mean[0][l_i];
	}
	
	// write floating coefs.
	l_row = "GainF(V/LSB)" + l_sep;
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_row += l_gain_f[l_i].ToString("F6") + l_sep;
	}
	x_strBCal.AppendLine(l_row);
	
	l_row = "OffsetF (LSB)" + l_sep;
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_row += l_offset_f[l_i].ToString("F6") + l_sep;
	}
	x_strBCal.AppendLine(l_row);
	
	
	// verification
	x_strBCal.AppendLine();
	x_strBCal.AppendLine("Verification");
	l_row = "ADCmin To V" + l_sep;
	double[] l_val = new double[l_nbVar];
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_val[l_i] = (x_mean[0][l_i] + l_offset_f[l_i])*l_gain_f[l_i]*l_a + l_b;
		l_row += l_val[l_i].ToString("F6") + l_sep;
	}	
	x_strBCal.AppendLine(l_row);
	
	l_row = "Diff ADCmin" + l_sep;
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		double l_diff = x_dmm_ref[0] - l_val[l_i];
		l_row += l_diff.ToString("F6") + l_sep;
	}	
	x_strBCal.AppendLine(l_row);
	
	l_row = "ADCmax To V" + l_sep;
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_val[l_i] = (x_mean[1][l_i] + l_offset_f[l_i])*l_gain_f[l_i]*l_a + l_b;
		l_row += l_val[l_i].ToString("F6") + l_sep;
	}	
	x_strBCal.AppendLine(l_row);
	
	l_row = "Diff ADCmax" + l_sep;
	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		double l_diff = x_dmm_ref[1] - l_val[l_i];
		l_row += l_diff.ToString("F6") + l_sep;
	}	
	x_strBCal.AppendLine(l_row);
}


// ---------------------------------------------------------------------------
// Extract data from ADC and write into CSV (same name)
// input: 
//		x_file:		csv filename, no suffix type
// 	x_nb: 		nb of iteration to get
// 	x_statOnly: statistics only in csv
//		x_dmm_ref:	dmm reference voltage
//		x_strBCal: 	string builder for calibration results
// returns: array of mean values measured
// ---------------------------------------------------------------------------
double[] ExtractAdcData(int x_nb, string x_file, bool x_statOnly, 
							System.Text.StringBuilder x_strBCal)
{
	const string l_sep = ";";
	const int l_nbVar = 8;
	
	double[] l_mean = new double[l_nbVar];
	
	//-------------------------------------------------
	// Prepare data
	//-------------------------------------------------
	string[] l_columns = new string[l_nbVar];
	string[] l_variable = new string[l_nbVar];

	for (int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_columns[l_i] = "HV" + l_i.ToString();
		l_variable[l_i] = "GPIO.GPIO-ADC-DPRAM.HV-Channels.CH" + l_i.ToString() + ".HV";
	}

	//l_columns[8] = "Global-HV";
	//l_variable[8] = "GPIO.GPIO-ADC-DPRAM.Others.Global-HV";

	var l_strBF = new  System.Text.StringBuilder(); // for float
	var l_strBU = new  System.Text.StringBuilder(); // for UInt32

	// build columns
	string l_row = "#" + l_sep;
	for(int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_row += l_columns[l_i] + l_sep;
	}
	l_strBF.AppendLine(l_row);
	l_strBU.AppendLine(l_row);
	x_strBCal.AppendLine(l_row);

	UInt32 l_nbWord = 0;
	List<double>[] l_listArrayF = new List<double>[l_nbVar]; // for float
	List<double>[] l_listArrayU = new List<double>[l_nbVar]; // for UInt32
	for(int l_i=0; l_i<l_nbVar; l_i++)
	{
		l_listArrayF[l_i] = new List<double>();
		l_listArrayU[l_i] = new List<double>();
	}
		
	//-------------------------------------------------
	// LOOP to read ADC values		
	//-------------------------------------------------
	
	// force a 1st dummy measurement
	BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart", false);
	BoardLib.UpdateUserParameters("GPIO.GPIO-ADC"); 
	
	Console.WriteLine("Starting iterations nb = " + x_nb);
	
	for(int l_iter = 0; l_iter<x_nb; l_iter++)
	{
		double l_val;
		string l_str;		
		
		BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
		Sync.Sleep(1);
		BoardLib.UpdateUserParameters("GPIO.GPIO-ADC-DPRAM");		
		for (int l_i=0; l_i<l_nbVar; l_i++)
		{			
			// for float
			l_str = BoardLib.GetFormulaVariable(l_variable[l_i]);
			if (Double.TryParse(l_str, out l_val))
            l_listArrayF[l_i].Add(l_val);
         else
            Console.WriteLine("Unable to parse " + l_variable[l_i] + "='" + l_str + "' at iteration #" + l_iter.ToString());
			
			// for UInt32
			l_listArrayU[l_i].Add((double)BoardLib.GetUInt32Variable(l_variable[l_i]));
		}
		if(l_iter%(x_nb/10) == 0)
			Console.WriteLine("Iteration #" + l_iter);
	}  	
	
	// find the nb of rows
	int l_maxRow = 0;
	for(int l_i=0; l_i<l_nbVar; l_i++)
	{
		if(l_listArrayU[l_i].Count > l_maxRow)
			l_maxRow = l_listArrayU[l_i].Count;
	}
	Console.WriteLine("Max rows = " + l_maxRow);
	
	//-------------------------------------------------
	// Build the raw data rows		
	//-------------------------------------------------
	if(!x_statOnly)
	{
		for(int l_r=0; l_r<l_maxRow; l_r++)
		{
			// --- Float ------------
			l_row = l_r.ToString() + l_sep;
			for(int l_i=0; l_i<l_nbVar; l_i++)
			{
				if(l_r<l_listArrayF[l_i].Count)
					l_row += l_listArrayF[l_i][l_r].ToString() + l_sep;
				else
					l_row += l_sep;
			}
			l_strBF.AppendLine(l_row);
			
			// --- UInt32 ------------
			l_row = l_r.ToString() + l_sep;
			for(int l_i=0; l_i<l_nbVar; l_i++)
			{
				if(l_r<l_listArrayU[l_i].Count)
					l_row += l_listArrayU[l_i][l_r].ToString() + l_sep;
				else
					l_row += l_sep;
			}
			l_strBU.AppendLine(l_row);
		}
		
		l_strBF.AppendLine();
		l_strBU.AppendLine();
	}
	
	//-------------------------------------------------
	// Statistics	
	//-------------------------------------------------
	l_strBF.AppendLine("Statistics");
	l_strBU.AppendLine("Statistics");
	x_strBCal.AppendLine("Statistics");
	
	string[] l_stats = new string[5];
	string[] l_tol = new string[5];
	
	// --- Float ------------
	l_stats[0] = "Average" + l_sep;
	l_stats[1] = "Std Dev." + l_sep;
	l_stats[2] = "Min" + l_sep;
	l_stats[3] = "Max" + l_sep;
	l_stats[4] = "Max-Min" + l_sep;
	
	l_tol[0] = "F5";
	l_tol[1] = "F6";
	l_tol[2] = "";
	l_tol[3] = "";
	l_tol[4] = "";
	
	for(int l_i=0; l_i<l_nbVar; l_i++)
	{
		var l_array = Statistics(l_listArrayF[l_i]);
		for(int l_j=0; l_j<5; l_j++)
		{
			l_stats[l_j] += l_array[l_j].ToString(l_tol[l_j]) + l_sep;
		}
	}
	
	for(int l_j=0; l_j<5; l_j++)
	{
		l_strBF.AppendLine(l_stats[l_j]);
	}	
	
	// --- UInt32 ------------
	l_stats[0] = "Average" + l_sep;
	l_stats[1] = "Std Dev." + l_sep;
	l_stats[2] = "Min" + l_sep;
	l_stats[3] = "Max" + l_sep;
	l_stats[4] = "Max-Min" + l_sep;
	
	l_tol[0] = "F5";
	l_tol[1] = "F6";
	l_tol[2] = "";
	l_tol[3] = "";
	l_tol[4] = "";
	
	for(int l_i=0; l_i<l_nbVar; l_i++)
	{
		var l_array = Statistics(l_listArrayU[l_i]);
		for(int l_j=0; l_j<5; l_j++)
		{
			l_stats[l_j] += l_array[l_j].ToString(l_tol[l_j]) + l_sep;
		}
		l_mean[l_i] = l_array[0];
	}
	
	for(int l_j=0; l_j<5; l_j++)
	{
		l_strBU.AppendLine(l_stats[l_j]);
		x_strBCal.AppendLine(l_stats[l_j]);
	}
	
	//-------------------------------------------------
	// Print report to CSV
	//-------------------------------------------------
	try
	{
		System.IO.File.WriteAllText(x_file + "_F.csv" , l_strBF.ToString());
		System.IO.File.WriteAllText(x_file + "_U.csv" , l_strBU.ToString());
	}
	catch
	{
		Console.WriteLine("ERROR : IO access during CSV write");
	}
	
	return l_mean;
}

// ---------------------------------------------------------------------------
// Compute statistics from a list
// input : 
//		x_listArray: list to be computed
// returns :
//		arrays of 5 doubles : Average / Sdt Dev / Min / Max / Max-Min
// ---------------------------------------------------------------------------
double[] Statistics(List<double> x_listArray)
{
	double[] l_array = new double[5];
	
	// find the nb of rows
	l_array[2] = 1e9; // min
	l_array[3] = 0; // max
	
	double l_sum = 0;
	
	// 1st loop for average, Min & Max
	for(int l_i=0; l_i<x_listArray.Count; l_i++)
	{		
		//Sum for Average
		l_sum += x_listArray[l_i];
		
		//Maximum
		if(x_listArray[l_i] > l_array[3])
			l_array[3] = x_listArray[l_i];
		
		//Minimum
		if(x_listArray[l_i] < l_array[2])
			l_array[2] = x_listArray[l_i];

	}
	// average
	l_array[0] = l_sum / x_listArray.Count;
	// Max - Min
	l_array[4] = l_array[3] - l_array[2];
	
	// 2nd loop for std. dev
	l_sum = 0;
	for(int l_i=0; l_i<x_listArray.Count; l_i++)
	{
		// Sum for std. dev
		l_sum += Math.Pow(x_listArray[l_i] - l_array[0], 2);
	}
	// std dev.
	l_array[1] = Math.Sqrt(l_sum / x_listArray.Count);
	
	return l_array;
}


