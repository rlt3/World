local Vec3_mt = {
    __index = {
        near = function (self, other)
            if (math.abs(self.x - other.x) > 0.1) then
                return false
            elseif (math.abs(self.y - other.y) > 0.1) then
                return false
            elseif (math.abs(self.z - other.z) > 0.1) then
                return false
            else
                return true
            end
        end,

        magnitude = function (self)
            return math.sqrt(self.x * self.x + self.y * self.y + self.z * self.z)
        end,

        normalize = function (self)
            local length = self:magnitude()
            return Vec3(self.x / length, self.y / length, self.z / length)
        end,
    },

    __mul = function (lhs, rhs)
        if type(rhs) == "number" then
            -- a scalar
            return Vec3(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs)
        else
            -- a cross product
            error("cross produce not implemented")
        end
    end,

    __add = function (lhs, rhs)
        return Vec3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z)
    end,

    __sub = function (lhs, rhs)
        return Vec3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z)
    end,

    __tostring = function (self)
        --return "(" .. self.x .. ", " .. self.y .. ", " .. self.z .. ")"
        return self.x .. "," .. self.y .. "," .. self.z
    end,

    __concat = function (lhs, rhs)
        return tostring(lhs) .. tostring(rhs)
    end
}

function Vec3 (x, y, z)
    local t = {
        x = x or 0,
        y = y or 0,
        z = z or 0,
    }
    setmetatable(t, Vec3_mt)
    return t
end
