#pragma once

class Operations
{

public:
    int f(int x) const;

    int g(int x) const;

    Operations();

    class Static_Singleton
    {

    private:
        static std::shared_ptr<Operations::Static_Singleton> Static_Singleton_instance_value;

        Static_Singleton();

    public:
        Vector2<int> ToVector(int A, int B) const;

        static std::shared_ptr<Operations::Static_Singleton> getInstance();

    };

};