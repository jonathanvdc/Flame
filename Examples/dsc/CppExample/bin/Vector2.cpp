#include <cmath>
#include "Vector2.h"

// Creates a new vector at the origin.
Vector2::Vector2()
{
    this->X = 0;
    this->Y = 0;
}

// Creates a new vector based on the given coordinates.
Vector2::Vector2(double X, double Y)
{
    this->X = X;
    this->Y = Y;
}

// Gets the vector's length.
double Vector2::getLength() const
{
    return std::hypot(this->X, this->Y);
}