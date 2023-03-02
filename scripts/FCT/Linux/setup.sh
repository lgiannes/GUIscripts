export GENERALDATADIR="/home/neutrino/FCT/FCT_database/"
[ ! -d $GENERALDATADIR ] && mkdir $GENERALDATADIR
export GUI_FOLDER="/home/neutrino/FCT/GUI_UT92"
export FCT_RUN_FOLDER="/home/neutrino/FCT/code/scripts/FCT/Linux"
export MIBDATADIR=$GENERALDATADIR"/MIBs/"
[ ! -d $MIBDATADIR ] && mkdir $MIBDATADIR
export FEBDATADIR=$GENERALDATADIR"/FEBs/"
[ ! -d $FEBDATADIR ] && mkdir $FEBDATADIR

export ANALYSIS_FOLDER="/home/neutrino/FCT/FunctionalTest/"

echo $GENERALDATADIR
