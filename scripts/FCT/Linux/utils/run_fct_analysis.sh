sn=$1
bl1=$2
bl2=$3


echo "data directory: "$DATADIR
[ -z $DATADIR ] && echo "data directory does not exist. Run acquisiton first" && exit

exe_path=$ANALYSIS_FOLDER"/bin/";
exe_calib="CalibrationPlots"
exe_analog="FuncTest";
exe_bl="FCTbaseline"
exe_citi="FCTcitiTriggers"
Data_file_name="FCT_os_LG56HG12amp30mV_bl32786.daq";
Data_file_name_bl1="FCT_BLTEST_LG56HG12amp30mV_baseline$bl1.daq"
Data_file_name_bl2="FCT_BLTEST_LG56HG12amp30mV_baseline$bl2.daq"
CITI_subfolder="/CITI_trigger_tests/"

setup_path=$ANALYSIS_FOLDER"/setup.sh";
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

if [[ $4 = "$str_cal" ]]
then
  echo
  echo "Not doing calibration analysis."
  echo
else
  echo
  echo "Calibration will be included"
  echo
  $exe_path$exe_calib -s $sn;
fi
#Open/Short and Basic Analog test
$exe_path$exe_analog -f $DATADIR$Data_file_name -s$sn;
#Baseline test
$exe_path$exe_bl -a $DATADIR$Data_file_name_bl1 -b $DATADIR$Data_file_name_bl2 -s$sn;
#CITIROC triggers test
$exe_path$exe_citi -f $DATADIR$CITI_subfolder -s$sn -v0 -a0;

echo 
echo "RESULTS:"
echo 
bash $FCT_RUN_FOLDER/ShowResults.sh $sn
