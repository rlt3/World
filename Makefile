#g++ -Wall --std=c++11 `pkg-config --cflags luajit` -o world world.cpp `pkg-config --libs luajit` -lnanomsg -ldl -lm
all: utilities world player

world: utilities
	mono-csc -r:Utilities.dll -lib:Utilities.dll -r:NNanomsg.dll -lib:NNanomsg.dll -main:Program World.cs

player: utilities
	mono-csc -r:Utilities.dll -lib:Utilities.dll Player.cs

utilities: Utilities.cs
	mono-csc -r:NNanomsg.dll -lib:NNanomsg.dll -target:library Utilities.cs
