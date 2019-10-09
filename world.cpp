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

/*
 * Update the entity like entity[key] = value and then broadcast that change to
 * all clients.
 * arg1 = entity table
 * arg2 = key in table
 * arg3 = value in table
 * returns nothing
 */
int
broadcast (lua_State *L)
{
    static const int entity = 1;
    static const int key = 2;
    static const int value = 3;
    const char *str;

    /*
     * Lua can more easily determine the proper type of its own objects to 
     * print them out than C can. So we build the broadcast string within Lua
     * itself. Once we have the built string then we can do what we want with
     * it later.
     */

    /* get the id from the entity and leave it on the stack */
    lua_pushstring(L, "id");
    lua_gettable(L, entity);

    /* push the values to concat them into our string */
    lua_pushstring(L, ".");
    lua_pushvalue(L, key);
    lua_pushstring(L, "=");
    lua_pushvalue(L, value);
    lua_concat(L, 5);

    /* string's lifetime is until it is popped */
    str = lua_tostring(L, -1);
    puts(str);
    str = NULL;
    lua_pop(L, 1);

    /* 
     * with the original arguments back in order, this can be called directly
     * to set the entity's key value.
     */
    lua_settable(L, entity);

    return 0;
}

/*
 * Lookup an entity by id. Returns the entity if exists, otherwise nil
 * arg1 = entity id
 * returns entity table or nil
 */
int
lookup_entity (lua_State *L)
{
    return 1;
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
    lua_pushcfunction(L, broadcast);
    lua_setglobal(L, "broadcast");
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
