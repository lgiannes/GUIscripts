#export GENERALDATADIR="/home/neutrino/FCT/FCT_retests/"
export GENERALDATADIR="/home/neutrino/FCT/FCT_WITH_COLDPLATES/"
# export GENERALDATADIR="/home/neutrino/FCT/FCT_burning_test/"
#export GENERALDATADIR="/home/neutrino/FCT/FCT_VST/"

### other directories settings
[ ! -d $GENERALDATADIR ] && mkdir $GENERALDATADIR
export GUI_FOLDER="/home/neutrino/FCT/GUI_UT92"
export FCT_RUN_FOLDER="/home/neutrino/FCT/code/scripts/FCT/Linux"
export FCT_UTILS=$FCT_RUN_FOLDER"/utils/"
export MIBDATADIR=$GENERALDATADIR"/MIBs/"
[ ! -d $MIBDATADIR ] && mkdir $MIBDATADIR
export FEBDATADIR=$GENERALDATADIR"/FEBs/"
[ ! -d $FEBDATADIR ] && mkdir $FEBDATADIR
export ANALYSIS_FOLDER="/home/neutrino/FCT/FunctionalTest/"
export CONFIGFOLDER="/home/neutrino/FCT/code/config/"
export GPIO_CALIB_FOLDER=$FCT_RUN_FOLDER"/GPIO_calib"

export WHICHSETUP="JPARC" # use ONLY "UNIGE" or "JPARC"

#export ip_address="10.195.52.177"
export ip_address="10.195.52.144"
export port="11000"
#export port="12000"

#check GPIO SN on the board
export GPIO_SN="41"
#max HV from the PS
export PS_HV="60"

#Load the configuration settings
source test_config.sh