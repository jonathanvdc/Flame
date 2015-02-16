#pragma once

class VectorTest
{

private:
    std::vector<int> data = std::vector<int>();

public:
    void Add(int Item);

    VectorTest();

    int getItem(int Index) const;
    void setItem(int Index, int value);

};