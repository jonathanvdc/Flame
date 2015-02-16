class Date:
    """ Represents a simple date. """

    def __init__(self, Day, Month, Year):
        """ Creates a new date from a day, month and year. """
        # Pre:
        # Day must be a positive integer that represents a valid day for the provided month.
        # Month must be an integer from 1 to 12.
        # Year must be an integer.
        self.day = Day
        self.month = Month
        self.year = Year

    def __str__(self):
        """ Gets this date's string representation. """
        return str(self.day) + "/" + str(self.month) + "/" + str(self.year)

    def compare(self, Other):
        """ Compares this 'Date' instance with another 'Date' instance. """
        # Post:
        # Returns 1 if 'self > Other', -1 if 'self < Other', otherwise 0.
        if self.year > Other.year:
            return 1
        elif self.year < Other.year:
            return -1
        elif self.month > Other.month:
            return 1
        elif self.month < Other.month:
            return -1
        elif self.day > Other.day:
            return 1
        elif self.day < Other.day:
            return -1
        else:
            return 0

    def __eq__(self, Other):
        """ Finds out if two 'Date' instances are equal. """
        return self.compare(Other) == 0

    def __ne__(self, Other):
        """ Finds out if two 'Date' instances are not equal. """
        return self.compare(Other) != 0

    def __lt__(self, Other):
        """ Finds out if this 'Date' instance is less than the given 'Date' instance. """
        return self.compare(Other) < 0

    def __gt__(self, Other):
        """ Finds out if this 'Date' instance is greater than the given 'Date' instance. """
        return self.compare(Other) > 0

    def __le__(self, Other):
        """ Finds out if this 'Date' instance is less than or equal to the given 'Date' instance. """
        return self.compare(Other) <= 0

    def __ge__(self, Other):
        """ Finds out if this 'Date' instance is greater than or equal to the given 'Date' instance. """
        return self.compare(Other) >= 0

    def __hash__(self):
        """ Gets a hash code for the current Date instance. """
        return self.day ^ self.month | self.year << 5

    @property
    def day(self):
        """ Gets the day of this date. """
        return self.day_value

    @day.setter
    def day(self, value):
        """ Sets the day of this date.
            This accessor is private. """
        self.day_value = value

    @property
    def month(self):
        """ Gets the month of this date. """
        return self.month_value

    @month.setter
    def month(self, value):
        """ Sets the month of this date.
            This accessor is private. """
        self.month_value = value

    @property
    def year(self):
        """ Gets the year of this date. """
        return self.year_value

    @year.setter
    def year(self, value):
        """ Sets the year of this date.
            This accessor is private. """
        self.year_value = value