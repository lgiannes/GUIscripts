sn=$1
bl1=$2
bl2=$3

if [[ $# -eq 4 && ($4 == 1) ]]
then 
  DATADIR="/home/neutrino/FCT/data_local/SN_"$sn
  echo "Running analysis only"
fi


exe_path="/home/neutrino/FCT/FunctionalTest/bin/";
exe_analog="FuncTest";
exe_bl="FCTbaseline"
exe_citi="FCTcitiTriggers"
Data_file_name="FCT_os_LG56HG12amp30mV_bl32786.daq";
Data_file_name_bl1="FCT_BLTEST_LG56HG12amp30mV_baseline$bl1.daq"
Data_file_name_bl2="FCT_BLTEST_LG56HG12amp30mV_baseline$bl2.daq"
CITI_subfolder="/CITI_trigger_tests/"

setup_path="/home/neutrino/FCT/FunctionalTest/setup.sh";
log_file=$DATADIR"/log.txt";


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
$exe_path$exe_analog -f $DATADIR$Data_file_name -s$sn;
#Baseline test
$exe_path$exe_bl -a $DATADIR$Data_file_name_bl1 -b $DATADIR$Data_file_name_bl2 -s$sn;

echo 
echo "RESULTS:"
echo 
bash ShowResults.sh $sn