#include <iostream>
#include <cstdlib>
#include <sstream>
#include <string>
#include <vector>
#include <chrono>

extern "C" {
#include <lua.h>
#include <lauxlib.h>
#include <lualib.h>
#include <unistd.h>
#include <math.h>
}

#include "Watch.hpp"

/*
 * Base class for entities. Eventually this will call the Lua scripts for a 
 * particular implementation of an entity. Those scripts will provide the
 * foundation for types of entities (Humans vs. Containers vs. Traps etc.)
 */
struct Entity {
    Watch last_touch;
    int id;
    float x, y, z;

    Entity (int id)
        : id(id)
        , x(0), y(0), z(0)
    { }
};

int
lua_newref (lua_State *L)
{
    return luaL_ref(L, LUA_REGISTRYINDEX);
}

void
lua_pushref (lua_State *L, int ref)
{
    lua_rawgeti(L, LUA_REGISTRYINDEX, ref);
}

void
fault (lua_State *L, const char *where)
{
    fprintf(stderr, "%s: %s\n", where, lua_tostring(L, -1));
    exit(1);
}

Entity
new_entity (lua_State *L)
{
    lua_newtable(L);
    int id = lua_newref(L);

    lua_getglobal(L, "init");

    /* while we have it on the stack, add its id to the table */
    lua_pushref(L, id);
    lua_pushstring(L, "id");
    lua_pushnumber(L, id);
    lua_settable(L, -3);

    if (lua_pcall(L, 1, 0, 0))
        fault(L, "new_entity");

    std::cout << "New Entity: " << id << std::endl;
    return Entity(id);
}

/*
 * Update an entity over a window of time.
 */
void
update_entity (lua_State *L, Entity& E)
{
    lua_getglobal(L, "update");
    lua_pushref(L, E.id);
    lua_pushnumber(L, E.last_touch.elapsed());
    if (lua_pcall(L, 2, 0, 0))
        fault(L, "update_entity");

    E.last_touch.reset();
}

void
load_script (lua_State *L, const char *script)
{
    luaL_loadfile(L, script);
    if (lua_pcall(L, 0, 0, 0) != 0)
        fault(L, "load_script");
}

int
main (int argc, char **argv)
{
    static const float SIXTY_FPS = 1.f/60.f;
    Watch Frames;

    lua_State *L = luaL_newstate();
    if (!L)
        return 1;
    luaL_openlibs(L);
    load_script(L, "script.lua");

    /*
     * Create new entities. This will soon have a 'script' argument which will
     * run a lua script, initializing the entity inside the VM
     */
    std::vector<Entity> entities;
    entities.push_back(new_entity(L));
    entities.push_back(new_entity(L));

    while (1) {
        if (Frames.elapsed() < SIXTY_FPS)
            continue;
        Frames.reset();
        for (auto &E : entities)
            update_entity(L, E);
    }

    lua_close(L);

    return 0;
}
