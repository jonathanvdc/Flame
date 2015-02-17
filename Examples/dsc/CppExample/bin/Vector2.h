#pragma once
#include <cmath>

// Describes a two-dimensional vector.
struct Vector2
{
    // The vector's X-coordinate.
    double X;

    // The vector's Y-coordinate.
    double Y;

    // Creates a new vector at the origin.
    Vector2();

    // Creates a new vector based on the given coordinates.
    Vector2(double X, double Y);

    // Gets the vector's length.
    double getLength() const;

};