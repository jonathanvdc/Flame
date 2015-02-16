#pragma once

template<typename T>
class Vector2
{

private:
    T X_value;

    T Y_value;

public:
    Vector2(T X, T Y);

    void setX(T value);
    T getX() const;

    void setY(T value);
    T getY() const;

};