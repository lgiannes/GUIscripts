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

#Launch the ROOT analyses.
#Setup
source $setup_path;
#Open/Short and Basic Analog test
$exe_path$exe_analog -f $DATADIR$Data_file_name -s$sn;
#Baseline test
$exe_path$exe_bl -a $DATADIR$Data_file_name_bl1 -b $DATADIR$Data_file_name_bl2 -s$sn;
#CITIROC triggers test
$exe_path$exe_citi -f $DATADIR$CITI_subfolder -s$sn -v0;

tail -n3 $DATADIR/output_*.txt
# LBHK_last_output=$( ls -tp $DATADIR/IO_TEST/ | grep -v / | head -n1 )
# tail -n3 $DATADIR/IO_TEST/$LBHK_last_output
tail -n3 $( ls $DATADIR/IO_TEST/ -tp | grep -v / | head -n1 )