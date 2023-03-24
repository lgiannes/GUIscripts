sn=$1



exe_path=$ANALYSIS_FOLDER/bin/;
exe_mib="MIBtest";
Data_file_name="MIB_"$sn"_openshort.daq"

setup_path="$ANALYSIS_FOLDER/setup.sh";


# echo $DATADIR
echo
echo " /------------------------\\"
echo " |                        |"
echo " |  Analysis starting ... |"
echo " |                        |"
echo " \------------------------/"
echo
#Launch the ROOT analyses.
#Setup
source $setup_path;
#Open/Short and Basic Analog test
$exe_path$exe_mib -f $MIBDATADIR$Data_file_name -s$sn;

rm -f $MIBDATADIR"output_os.txt"
#rm -f $MIBDATADIR"MIB_"$sn"_EndOfScript.txt"
echo 
echo "Results in: MIB_"$sn"_TestResult.txt"
echo "---------------------------------------------------"
head $MIBDATADIR"MIB_"$sn"_TestResult.txt"
echo "---------------------------------------------------"
