from IRecord import *

class Auditorium(IRecord):
    """ Describes an auditorium. """

    def __init__(self, Index, NumberOfSeats):
        """ Creates a new auditorium instance for the provided index and number of seats. """
        # Pre:
        # 'Index' must be a unique index and 'NumberOfSeats' must be a nonzero positive integer.
        self.index = Index
        self.number_of_seats = NumberOfSeats

    def __str__(self):
        """ Gets the auditorium's string representation. """
        return "Auditorium " + str(self.index) + " (" + str(self.number_of_seats) + " seats)"

    def __eq__(self, Other):
        """ Finds out if this auditorium equals the given auditorium. """
        if Other is None:
            return False
        else:
            return self.index == Other.index

    def __ne__(self, Other):
        """ Finds out if this auditorium is not equal to the given auditorium. """
        return not self == Other

    @property
    def index(self):
        """ Gets the auditorium's index, or room number. """
        return self.index_value

    @index.setter
    def index(self, value):
        """ Sets the auditorium's index, or room number.
            This accessor is private. """
        self.index_value = value

    @property
    def number_of_seats(self):
        """ Gets the number of seats in the auditorium. """
        return self.number_of_seats_value

    @number_of_seats.setter
    def number_of_seats(self, value):
        """ Sets the number of seats in the auditorium.
            This accessor is private. """
        self.number_of_seats_value = value

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.index