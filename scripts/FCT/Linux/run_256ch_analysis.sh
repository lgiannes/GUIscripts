
#Launch the ROOT analyses
source $setup_path;
#Open/Short and Basic Analog
$exe_path$exe_analog -f $Data_path$Data_file_name -s$sn;
#Baseline test
$exe_path$exe_bl -a $Data_path$Data_file_name_bl1 -b $Data_path$Data_file_name_bl2 -s$sn;

