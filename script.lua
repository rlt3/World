require "Vec3"

function next_path_point (E)
    E.position = E.path[E.path_point];
    if (E.path_point >= #E.path) then
        E.path_point = 1;
    else
        E.path_point = E.path_point + 1
    end
    E.direction = (E.path[E.path_point] - E.position):normalize();
end

function init (E)
    x = math.random(5)
    y = math.random(5)
    z = math.random(5)
    E.position = nil
    E.direction = nil
    E.path_point = 1
    E.move_per_second = 5.0
    E.path = {
        Vec3(x + 0,  y + 0,  z + 0),
        Vec3(x + 25, y + 0,  z + 0),
        Vec3(x + 25, y + 25, z + 0),
        Vec3(x + 0,  y + 25, z + 0)
    }
    next_path_point(E)
end

--
-- Broadcast an entity state change that should be known by the client
--
function broadcast (E, key, value)
    E[key] = value
    print(E.id .. "." .. key .. "=" .. value)
end

function update (E, dt)
    broadcast(E, "position", E.position + (E.direction * (E.move_per_second * dt)))
    if (E.position:near(E.path[E.path_point])) then
        next_path_point(E)
    end
end
