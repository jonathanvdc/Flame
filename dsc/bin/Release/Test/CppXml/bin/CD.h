#pragma once
#include <string>

class CD
{

private:
    int Year_value;

    std::string Title_value;

    std::string Artist_value;

    std::string Country_value;

    std::string Company_value;

    double Price_value;

public:
    std::string ToString() const;

    CD();

    CD(std::string Title, std::string Artist, std::string Country, std::string Company, int Year, double Price);

    CD(std::string Title, std::string Artist);

    int getYear() const;
    void setYear(int value);

    std::string getTitle() const;
    void setTitle(std::string value);

    std::string getArtist() const;
    void setArtist(std::string value);

    std::string getCountry() const;
    void setCountry(std::string value);

    std::string getCompany() const;
    void setCompany(std::string value);

    double getPrice() const;
    void setPrice(double value);

};