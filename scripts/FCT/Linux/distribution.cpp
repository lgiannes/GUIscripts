#include <iostream>
#include <fstream>
#include <sstream>
#include <vector>

#include "TH1.h"
#include "TCanvas.h"

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
    canvas->SaveAs((title+".png").c_str());

    delete histogram;
    delete canvas;
}

void myMacro(int channel=0) {
    std::vector<double> column1, column2;

    std::string filename = "RawValues_max_verify_SN52.csv";  // Replace with the actual file path
    
    int SN;
    string snString;
    // Find the position of "SN" in the filename
    size_t pos = filename.find("SN");
    if (pos != std::string::npos) {
        // Extract the substring after "SN"
        snString = filename.substr(pos + 2);
        // Remove any non-digit characters from the substring
        snString.erase(std::remove_if(snString.begin(), snString.end(), [](char c) { return !std::isdigit(c); }), snString.end());
    }
    // Convert the extracted substring to an integer
    std::istringstream iss(snString);
    iss >> SN;


    ReadCSVFile(filename, column1, column2, channel);

    CreateHistograms(column1, ("SN"+to_string(SN)+"_HV"+to_string(channel)).c_str(), "HV [ADC]");
    CreateHistograms(column2, ("SN"+to_string(SN)+"_T"+to_string(channel) ).c_str(), " T [ADC]");
}
