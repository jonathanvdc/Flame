#include "VectorTest.h"


void VectorTest::Add(int Item)
{
    this->data.push_back(Item);
}

VectorTest::VectorTest()
{
    this->data = std::vector<int>();
}

int VectorTest::getItem(int Index) const
{
    return this->data.get_Item(Index);
}
void VectorTest::setItem(int Index, int value)
{
    this->data.set_Item(Index, value);
}