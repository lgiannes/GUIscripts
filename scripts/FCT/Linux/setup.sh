export GENERALDATADIR="/home/neutrino/FCT/FCT_database/"
[ ! -d $GENERALDATADIR ] && mkdir $GENERALDATADIR
export GUI_FOLDER="/home/neutrino/FCT/GUI_UT92"
export FCT_RUN_FOLDER="/home/neutrino/FCT/code/scripts/FCT/Linux"
export FCT_UTILS="/home/neutrino/FCT/code/scripts/FCT/Linux/utils"
export MIBDATADIR=$GENERALDATADIR"/MIBs/"
[ ! -d $MIBDATADIR ] && mkdir $MIBDATADIR
export FEBDATADIR=$GENERALDATADIR"/FEBs/"
[ ! -d $FEBDATADIR ] && mkdir $FEBDATADIR

export ANALYSIS_FOLDER="/home/neutrino/FCT/FunctionalTest/"
export CONFIGFOLDER="/home/neutrino/FCT/code/config/"
echo $GENERALDATADIR

#export ip_address="10.195.52.177"
export ip_address="10.195.52.144"
export port="11000"
#export port="12000"
