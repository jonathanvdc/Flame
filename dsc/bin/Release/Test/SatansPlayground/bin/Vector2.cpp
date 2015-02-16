#include "Vector2.h"



template<typename T>
Vector2<T>::Vector2(T X, T Y)
{
    this->setX(X);
    this->setY(Y);
}

template<typename T>
void Vector2<T>::setX(T value)
{
    this->X_value = value;
}
template<typename T>
T Vector2<T>::getX() const
{
    return this->X_value;
}

template<typename T>
void Vector2<T>::setY(T value)
{
    this->Y_value = value;
}
template<typename T>
T Vector2<T>::getY() const
{
    return this->Y_value;
}