require "Vec3"

function next_path_point (E)
    if (E.path_point >= #E.path) then
        E.path_point = 1;
    else
        E.path_point = E.path_point + 1
    end
    E.direction = (E.path[E.path_point] - E.position):normalize();
end

function init (E)
    --x = math.random(5)
    --y = math.random(5)
    --z = math.random(5)
    E.position = nil
    E.direction = nil
    E.path_point = 1
    E.move_per_second = 1.0
    E.path = {
        --Vec3(0,0,0),
        --Vec3(-4,0,0),
        --Vec3(-4,0,3),
        --Vec3(0,0,3)
        Vec3(2,0,-12),
        Vec3(-2,0,-12),
        Vec3(-2,0,-9),
        Vec3(2,0,-9)
        --Vec3(x + 0,  y + 0,  z + 0),
        --Vec3(x + 25, y + 0,  z + 0),
        --Vec3(x + 25, y + 25, z + 0),
        --Vec3(x + 0,  y + 25, z + 0)
    }
    E.position = E.path[E.path_point]
    next_path_point(E)
end

function update (E, dt)
    -- 
    -- Instead of sending updates of every minute detail send instead the broad
    -- strokes. Meaning, send the path information itself. There's still a need
    -- to compute it server side (the locations that is) so that the correct
    -- information can be sent to all clients, even those that see an event
    -- later than another client.
    --
    local direction = (E.direction * (E.move_per_second * dt))
    E.position  = E.position + direction
    --broadcast(E, "position", direction)
    broadcast(E, "position", E.position)
    if (E.position:near(E.path[E.path_point])) then
        next_path_point(E)
    end
end
