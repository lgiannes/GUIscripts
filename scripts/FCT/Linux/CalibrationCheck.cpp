void ReadCSVFile(const std::string& filename, std::vector<double>& column1, std::vector<double>& column2,int channel) {
    std::ifstream file(filename);
    if (!file) {
        std::cout << "Error opening file: " << filename << std::endl;
        return;
    }

    std::string line;
    // skip first line
    std::getline(file, line);
    while (std::getline(file, line)) {


        std::istringstream iss(line);
        std::string token;
        std::vector<std::string> tokens;

        while (std::getline(iss, token, ';')) {
            tokens.push_back(token);
        }
        if (std::stod(tokens[0])==channel){
            if (tokens.size() >= 2) {
                column1.push_back(std::stod(tokens[1]));
                column2.push_back(std::stod(tokens[2]));
            }
        }

    }

    file.close();
}


void CreateHistograms(const std::vector<double>& data, const std::string& title, const std::string& xlabel) {
    int numBins = 40;
    double minValue = *std::min_element(data.begin(), data.end());
    double maxValue = *std::max_element(data.begin(), data.end());
    double median = (minValue + maxValue) / 2;

    TH1D* histogram = new TH1D("histogram", title.c_str(), numBins, minValue-50, maxValue+50);

    for (const auto& value : data) {
        histogram->Fill(value);
    }

    histogram->GetXaxis()->SetTitle(xlabel.c_str());
    histogram->GetYaxis()->SetTitle("Frequency");

    cout<<"Average: "<<histogram->GetMean()<<endl;
    cout<<"RMS: "<<histogram->GetRMS()<<endl;

    TCanvas* canvas = new TCanvas("canvas", "Histogram", 800, 600);
    histogram->Draw();

    canvas->Update();
    // canvas->SaveAs((title+".png").c_str());

    // delete histogram;
    // delete canvas;
}


int CalibrationCheck(){
    // Ask for the SN
    std::string SN;
    std::cout << "Enter the SN: ";
    std::cin >> SN;
    // Get the environment variable GENERALDATADIR
    std::string generalDataDir = std::getenv("GENERALDATADIR");
    if (generalDataDir.empty()) {
        std::cout << "Error: Environment variable GENERALDATADIR is not set." << std::endl;
        std::cout << "Do source setup.sh" << std::endl;
        return 1;
    }

    // Find the calibration files
    std::string calibrationFile_M = generalDataDir + "FEBs/SN_" + SN + "/Calibration/RawValues_max.csv";
    std::string calibrationFile_m = generalDataDir + "FEBs/SN_" + SN + "/Calibration/RawValues_min.csv";
    // Read the calibration files


    int numBins = 40;

    // Create 8 histograms for T and 8 histograms for HV
    // Then save all in the same canvas
    TH1F* HVhisto[8];
    TH1F* Thisto[8];

    TCanvas* canvas = new TCanvas("canvas", "HV and T measurements", 1920, 1200);
    canvas->Divide(4,4);

    for(int ch=0;ch<8;ch++){
        std::vector<double> HV_M;
        std::vector<double> T_M;
        std::vector<double> HV_m;
        std::vector<double> T_m;
        ReadCSVFile(calibrationFile_M, HV_M, T_M,ch);
        
        double minValue = *std::min_element(HV_M.begin(), HV_M.end());
        double maxValue = *std::max_element(HV_M.begin(), HV_M.end());
        double median = (minValue + maxValue) / 2;
        double minValue_T = *std::min_element(T_M.begin(), T_M.end());
        double maxValue_T = *std::max_element(T_M.begin(), T_M.end());
        double median_T = (minValue_T + maxValue_T) / 2;
        

        //ReadCSVFile(calibrationFile_m, HV_m, T_m,ch);
        HVhisto[ch] = new TH1F(("HV"+to_string(ch)).c_str(), "HVhisto", numBins, median-50, median+50);
        Thisto[ch] = new TH1F(("T"+to_string(ch)).c_str(), "Thisto", numBins, median_T-50, median_T+50);
        for (int i=0;i<HV_M.size();i++) {
            HVhisto[ch]->Fill(HV_M[i]);
            Thisto[ch]->Fill(T_M[i]);
        }
        HVhisto[ch]->GetXaxis()->SetTitle("HV [ADC]");
        HVhisto[ch]->GetYaxis()->SetTitle("Entries");
        Thisto[ch]->GetXaxis()->SetTitle("T [ADC]");
        Thisto[ch]->GetYaxis()->SetTitle("Entries");
        HVhisto[ch]->SetLineColor(kRed);
        Thisto[ch]->SetLineColor(kBlue);
        HVhisto[ch]->SetTitle(("HV channel "+to_string(ch)).c_str());
        Thisto[ch]->SetTitle(("T channel "+to_string(ch)).c_str());

        canvas->cd(ch+1);
        HVhisto[ch]->Draw();
        canvas->cd(ch+9);
        Thisto[ch]->Draw();
        canvas->Update();

    }

    return 0;
}