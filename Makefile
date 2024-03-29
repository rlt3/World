all: world player

old:
	g++ -Wall --std=c++11 `pkg-config --cflags luajit` -o world world.cpp `pkg-config --libs luajit` -lnanomsg -ldl -lm

world: utilities script
	mono-csc \
		-r:Utilities.dll -lib:Utilities.dll \
		-r:Script.dll -lib:Script.dll \
		-r:NNanomsg.dll -lib:NNanomsg.dll \
		-sdk:4 -main:Program World.cs

player: utilities script
	mono-csc \
		-r:Utilities.dll -lib:Utilities.dll \
		-r:Script.dll -lib:Script.dll \
		-sdk:4 Player.cs

script: Script.cs
	mono-csc -sdk:4 -target:library Script.cs

utilities: script Utilities.cs
	mono-csc \
		-r:Script.dll -lib:Script.dll \
		-r:NNanomsg.dll -lib:NNanomsg.dll \
		-sdk:4 -target:library Utilities.cs
