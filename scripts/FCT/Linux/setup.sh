### Mother directory
#export GENERALDATADIR="/DATA/FCT_retests_y/"
#export GENERALDATADIR="/DATA/FCT_WITH_COLDPLATES/"
export GENERALDATADIR="/DATA/FCT_RETEST_FEB_FROMJAPAN/"

### other directories settings
[ ! -d $GENERALDATADIR ] && mkdir $GENERALDATADIR
export GUI_FOLDER="/home/neutrino/FCT/GUI"
export FCT_RUN_FOLDER="/home/neutrino/FCT/software/GUIscripts/scripts/FCT/Linux"
export FCT_UTILS=$FCT_RUN_FOLDER"/utils/"
export MIBDATADIR=$GENERALDATADIR"/MIBs/"
[ ! -d $MIBDATADIR ] && mkdir $MIBDATADIR
export FEBDATADIR=$GENERALDATADIR"/FEBs/"
[ ! -d $FEBDATADIR ] && mkdir $FEBDATADIR
export ANALYSIS_FOLDER="/home/neutrino/FCT/software/FunctionalTest/"
export CONFIGFOLDER="/home/neutrino/FCT/software/GUIscripts/config/"
export GPIO_CALIB_FOLDER=$FCT_RUN_FOLDER"/GPIO_calib"

### the following should match the IP address and PORT indicated on the app-settings.json file (lines 116-122) in the GUI folder
export ip_address="10.195.52.177"
#export ip_address="10.195.52.144"
#export port="11000"
export port="12000"

export GPIO_SN="44"


export PS_HV="60"

### test configuration 
#                       Default: 
#                               CONFIGNAME="config_FCT2_newGUI_V2.xml"
#                               amp="0.030" #V
#                               HG="12"
#                               LG="56"
#                               DAC10B="300"

export CONFIGNAME="config_FCT2_newGUI_V2.xml"
export amp="0.030" #V
export HG="12"
export LG="56"
export DAC10B="300"