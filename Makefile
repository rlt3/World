all:
	g++ -Wall --std=c++11 `pkg-config --cflags luajit` -o world world.cpp `pkg-config --libs luajit` -lnanomsg -ldl -lm
