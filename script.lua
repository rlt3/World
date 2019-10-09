require "vec3"

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
    E.position = nil
    E.direction = nil
    E.path_point = 1
    E.move_per_second = 5.0
    E.path = {
        Vec3(0, 0, 0),
        Vec3(25, 0, 0),
        Vec3(25, 25, 0),
        Vec3(0, 25, 0)
    }
    next_path_point(E)
end

function update (E, dt)
    E.position = E.position + (E.direction * (E.move_per_second * dt))
    if (E.position:near(E.path[E.path_point])) then
        next_path_point(E)
    end
    print(E.position)
end
