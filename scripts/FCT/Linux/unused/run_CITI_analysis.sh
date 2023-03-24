sn=$1
bl1=$2
bl2=$3

if [[ $# -eq 4 && ($4 == 1) ]]
then 
  DATADIR=$GENERALDATADIR"/SN_"$sn
  echo "Running analysis only"
fi


exe_path=$ANALYSIS_FOLDER"/bin/";
exe_analog="FuncTest";
exe_bl="FCTbaseline"
exe_citi="FCTcitiTriggers"
Data_file_name="FCT_os_LG56HG12amp30mV_bl32786.daq";
Data_file_name_bl1="FCT_BLTEST_LG56HG12amp30mV_baseline$bl1.daq"
Data_file_name_bl2="FCT_BLTEST_LG56HG12amp30mV_baseline$bl2.daq"
CITI_subfolder="/CITI_trigger_tests/"

setup_path="$ANALYSIS_FOLDER/setup.sh";
log_file=$DATADIR"/log.txt";


# echo $DATADIR

#Launch the ROOT analyses.
#Setup
source $setup_path;


#CITIROC triggers test
$exe_path$exe_citi -f $DATADIR$CITI_subfolder -s$sn -v0;

echo 
echo "RESULTS:"
echo 
bash $FCT_RUN_FOLDER/ShowResults.sh $sn