#!/bin/bash
bl1=32000
bl2=50000
source $FCT_RUN_FOLDER/setup.sh

if [[ -z $1 ]]
then
    echo tell me the Serial Number
    exit
fi
sn=$1
export str_cal="NOCALIB"
export DATADIR=$GENERALDATADIR"FEBs/SN_"$sn"/"
if [[ ! -d $DATADIR ]]
then 
    echo "$DATADIR :folder not found"
    exit 
fi
    echo
    echo "Running analysis on existing files"
    echo
    bash "$FCT_UTILS/run_fct_analysis.sh" $sn $bl1 $bl2
    exit
