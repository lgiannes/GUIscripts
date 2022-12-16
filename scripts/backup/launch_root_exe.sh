#source setup.sh
#export EXEDIR="Network/wsl$/Ubuntu/home/neutrino_ubuntu/Root_SW_FCTEST/sFGD_unpacking/bin";
export EXEDIR="home/neutrino_ubuntu/Root_SW_FCTEST/sFGD_unpacking/bin";

#export DATADIR="/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/root_exe/daq_test_root_exe";
export DATADIR="~";
echo $EXEDIR        $DATADIR> echo.txt
./$EXEDIR/unpack -f $DATADIR/*.daq > output.txt;