# RUN THE SCRIPTS. The list of all the scripts and description follows
**WARNING: Never run scripts with source or ./**
**Always run them in a subshell (bash ...)**
**it is good practice to work as root privileged user. run the following:**

sudo -i

**and insert password**

**Then move to the scripts folder (if that has been saved in the .bashrc):**

cd $FCT_RUN_FOLDER 

## Complete Functional test
**the main script of this package is Functional_Test.sh**
**it can be run without arguments to have a full functional test + calibration of the board:**

time bash Functional_Test.sh

**duration: around 5'30"**
**For the first test of the boards, before installation of the coldplate, the FCT has to be launched WITHOUT calibration. You can do so by adding the argument "NOCALIB":**

time bash Functional_Test.sh NOCALIB

**duration: around 6'30"**

## Housekeeping/Loopback only
**this script runs the first part of the test. Some hardware actions are required!**
**provide serial number as argument!**

time bash FCT_LBHK_test.sh SN

## 256ch test
**this script tests all the 256 analog channels and the well functioning of baseline change**

time bash FCT_256ch_test.sh

## FCT_CITIROC_test.sh
**this script tests the different trigger options of the CITIROCs**
**provide serial number as argument!**

time bash FCT_CITIROC_test.sh SN

## Calibration
**this sript runs the calibration routine for the FEB**
**provide serial number as argument!**

time bash FCT_Calibration.sh SN

## Analysis only
**this script runs teh analysis for a certain serial number for which the data taking part of the test (or the whole test) has already been done**
**provide serial number as argument!**

time bash Analysis_only.sh SN

## MIB test
**this script test the MIB. Warning: this is supposed to be run with the MIB type adapter board (look at the label at the bottom lef tof the big adapter board (connected to the GPIO))**

time bash MIBtest.sh 

## Show Results
**this script shows the results for a certain serial number for which the FCT (complete) has alrady been done**
**provide serial number as argument!**

bash ShowResults.sh SN

**this is automatically launched at the end of each of the scripts above**

**it is always good practice to source setup.sh in the terminal where the scripts are being ran all these scripts use subscripts and c sharp code contained in the ./utils/ folder**

---
# Test Bench set up

## INSTALL GUI
install the dependencies (partially following the instructions in: https://partphys.unige.ch/~favrey/Misc/UnigeGpioBoard/Install.txt)
   install monodevelop:
	sudo apt install apt-transport-https dirmngr
	sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
	echo "deb https://download.mono-project.com/repo/ubuntu vs-bionic main" | sudo tee /etc/apt/sources.list.d/mono-official-vs.list
	 sudo apt update
    install libUSB
	 sudo apt-get install libusb-1.0-0-dev
    make usb accessible from all users
	navigate to the folder /etc/udev/rules.d/ and create WITH USER PRIVILEGES a text file named 89-bmfeb.rules
	put the following line in the file and save it
	SUBSYSTEMS=="usb", ATTRS{idVendor}=="206b", GROUP="neutrino", MODE="0660"
	in a root privileged terminal, run the following to force reload of devices:
	udevadm control --reload-rules && udevadm trigger 	

### install the GUI from https://partphys.unige.ch/~favrey/SFGD/SFGD-GPIO/ 
	file: SFGD_GPIOFrontEnd-v951-linux.zip
	unzip it to a folder (eg ..../GUI/)
	Copy the json files in this shared folder https://drive.switch.ch/index.php/s/gc07AWr8Cv2iY5a 
	into the GUI folder. The password to access the shared folder is FCT
	
 
## PROGRAM THE BOARDS
	download quartus programmer for Linux:
	download files in:
		https://llrbox.in2p3.fr/owncloud/index.php/s/ywaVEVcpqgFqY9N
		Run the .run file and follow the instructions
		copy the following text into the file: /etc/udev/rules.d/51-usbblaster.rules
			# USB Blaster
			SUBSYSTEM=="usb", ENV{DEVTYPE}=="usb_device", ATTR{idVendor}=="09fb", ATTR{idProduct}=="6001", MODE="0666", NAME="bus/usb/$env{BUSNUM}/$env{DEVNUM}", RUN+="/bin/chmod 0666 %c"
			SUBSYSTEM=="usb", ENV{DEVTYPE}=="usb_device", ATTR{idVendor}=="09fb", ATTR{idProduct}=="6002", MODE="0666", NAME="bus/usb/$env{BUSNUM}/$env{DEVNUM}", RUN+="/bin/chmod 0666 %c"
			SUBSYSTEM=="usb", ENV{DEVTYPE}=="usb_device", ATTR{idVendor}=="09fb", ATTR{idProduct}=="6003", MODE="0666", NAME="bus/usb/$env{BUSNUM}/$env{DEVNUM}", RUN+="/bin/chmod 0666 %c"
			# USB Blaster II
			SUBSYSTEM=="usb", ENV{DEVTYPE}=="usb_device", ATTR{idVendor}=="09fb", ATTR{idProduct}=="6010", MODE="0666", NAME="bus/usb/$env{BUSNUM}/$env{DEVNUM}", RUN+="/bin/chmod 0666 %c"
			SUBSYSTEM=="usb", ENV{DEVTYPE}=="usb_device", ATTR{idVendor}=="09fb", ATTR{idProduct}=="6810", MODE="0666", NAME="bus/usb/$env{BUSNUM}/$env{DEVNUM}", RUN+="/bin/chmod 0666 %c"
		sudo ln -s /lib64/libudev.so.1 /lib64/libudev.so.0
		Restart PC
	Program the GPIO: 
		download firmware at: https://partphys.unige.ch/~favrey/SFGD/FW%20FCT/GPIO/
	Program the FEB:
		download the firmware at https://partphys.unige.ch/~favrey/SFGD/FW%20FCT/UT_92/
		file: ut92_V1.9-V1067.jic (Use .jic for permanent programming, .sof to program until power cycle)

## INSTALL ROOT
	download required dependencies:
	 sudo apt-get install dpkg-dev cmake g++ gcc binutils libx11-dev libxpm-dev \
	libxft-dev libxext-dev python libssl-dev
	install binay distribution for ROOT6 (6 or higher):
	go to the folder wher eyou want your ROOT to be (let's call it $ROOTFOLDER).
	 https://root.cern/download/root_v6.26.10.Linux-ubuntu22-x86_64-gcc11.3.tar.gz
	 tar -xzvf root_v6.26.10.Linux-ubuntu22-x86_64-gcc11.3.tar.gz 
	 source root/bin/thisroot.sh
	and add this last line to your ~/.bashrc file (don't forget to sfecify the path there!!)
	check that root is installed by typing "root" in whichever terminal

## CLONE SOFTWARE REPOSITORIES
	install git
	 sudo apt install git
	create a directory for the software and navigate into it
	clone analysis repository
	 git clone https://github.com/lgiannes/FunctionalTest.git
	clone GUI scripts repository
	 git clone https://github.com/lgiannes/GUIscripts.git
	REMARK: The code is private, you need to have GitHub account and have access to the repositories to clone them. Ask lorenzo.giannessi@unige.ch for permission

## COMPILE THE SOFTWARE
	install the dependencies: You need C++17 and CMake2.8.12 minimum
	navigate to FunctionalTest/build and run:
	 bash cmake_clean.sh
	 cmake ..
	 make
	
	modify the setup.sh files in both repositories according to your machine!
 
## LT SUPERVISOR PROGRAMMER
	for this, you need windows: install Oracle VirtualBox from https://www.virtualbox.org/wiki/Linux_Downloads (select the correct Ubuntu version)
	Open the virtual box and create a windows partition. (check that virtualization is enabled for your device, ask Yann Meunier for help...)
	IMPORTANT: you'll need to enable USB devices in the virtualbox, you can do so by typing sudo adduser $USERvboxusers in the terminal, then reboot (or re-login). 
	then go to the virtualbox window, click on Settings->USB->Add device and add all the devices you ned (especially LINEAR TECH LTC ...)
	Install LT programmer software at this page:
	https://www.analog.com/en/design-center/ltpower-play.html
	You have to request a (free!) license that they'll send you by email in order to use LT PowerPlay	
	download the files in this folder and put them in a dedicated folder https://drive.switch.ch/index.php/s/ADUFEtVxJqEhqb5
	if everything is set and connected (FEB enabled, dongle connected) it will be enough to double click on the batch file "config_ltc_FEB"		

## UTILITIES:
	1. it is recommended to install an IDE (even a simple one is ok)
	to install Visual Studio Code:
		 sudo apt-get install ./code_1.76.0-1677667493_amd64.deb 
	2. set up environment variables:
	add the following lines at the bottom of your ~/.bashrc file (take care of using the right paths!!! they may change depending on the computer):	
	export FCT_FOLDER="/path/to/GUIscripts/scripts/FCT/Linux/"
	export GUI_FOLDER="/path/to/GUI/"
		

---

### DEBUG HINTS:
**1**

Error : 'Object reference not set to an instance of an object' -> GPIO might be OFF
Check that the socket window says: "waiting for a new single client connection"

WARNING: it is MANDATORY to connect the GPIO to a USB3 port. USB2 is not supported.

**2** 

If the socket server stops at start up (you get "server: stopped" when you open the GUI), try to change the number of the port from the app-settings.json file contained in the GUI directory (same directory as UnigeGpioBoard.exe) Numbers accepted ONLY above 10000

**3**

The routine works with the pulse generator TG5012A from TTi, if you need to use another model of pulse gneerator, you might need to change the scripts to communicate with the pulse gen.
fnd the device manual here: http://resources.aimtti.com/manuals/TG5012A_2512A_5011A+2511A_Instructions-Iss4.pdf
CHECK COMMUNICATION WITH PULSE GEN:
	you should see the file ttyACM0 appearing in your /dev/ folder.
	enable writing to the device with:	
		sudo chmod a+w ttyACM0
