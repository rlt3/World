#pragma once

typedef std::chrono::high_resolution_clock Clock;
typedef std::chrono::milliseconds Elapsed_ms;
typedef std::chrono::duration<float> Elapsed;
typedef std::chrono::time_point<Clock> Timestamp;

class Watch {
public:
    Watch ()
    {
        start();
    }

    void
    start ()
    {
        running = true;
        reset();
    }

    void
    reset ()
    {
        t1 = Clock::now();
    }

    void
    stop ()
    {
        running = false;
        t2 = Clock::now();
    }

    float
    elapsed ()
    {
        Elapsed e;
        if (running)
            e = Clock::now() - t1;
        else
            e = t2 - t1;
        return e.count();
    }

protected:
    bool running;
    Timestamp t1;
    Timestamp t2;
};
