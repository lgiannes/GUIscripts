
#######################################################################################
### TEST CONFIGURATION 
#   Default: 
#           CONFIGNAME="config_FCT2_newGUI_V2.xml"
#           amp="0.060" #V
#           HG="12"
#           LG="56"
#           DAC10B="300"

export CONFIGNAME="config_FCT2_newGUI_V2.xml"
export amp="0.060" #V
export HG="12"
export LG="56"
export DAC10B="300" # set this to 0 to use the DAC10B set in the config file.

# skip parts of the functional test (0: perform test, 1: skip test)
export NO_CALIB=0
export NO_CITI_TRIG=0
export NO_CH_TEST=0

#######################################################################################
