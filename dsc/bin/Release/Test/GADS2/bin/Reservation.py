from IRecord import *

class Reservation(IRecord):
    """ Describes a reservation at a movie theater by a registered customer. """

    def __init__(self, Id, Customer, Showtime, Timestamp, NumberOfSeats):
        """ Creates a new reservation object from the provided arguments. """
        self.id = Id
        self.customer = Customer
        self.showtime = Showtime
        self.timestamp = Timestamp
        self.number_of_seats = NumberOfSeats

    def __str__(self):
        """ Gets the reservation's string representation. """
        return "Reservation #" + str(self.id) + ", made at " + str(self.timestamp) + ": " + str(self.number_of_seats) + " seats for " + str(self.showtime)

    @property
    def id(self):
        """ Gets the reservation's unique identifier. """
        return self.id_value

    @id.setter
    def id(self, value):
        """ Sets the reservation's unique identifier.
            This accessor is private. """
        self.id_value = value

    @property
    def customer(self):
        """ Gets the customer that has reserved a seat. """
        return self.customer_value

    @customer.setter
    def customer(self, value):
        """ Sets the customer that has reserved a seat.
            This accessor is private. """
        self.customer_value = value

    @property
    def showtime(self):
        """ Gets the showtime associated with this reservation. """
        return self.showtime_value

    @showtime.setter
    def showtime(self, value):
        """ Sets the showtime associated with this reservation.
            This accessor is private. """
        self.showtime_value = value

    @property
    def timestamp(self):
        """ Gets the date and time at which the reservation was processed. """
        return self.timestamp_value

    @timestamp.setter
    def timestamp(self, value):
        """ Sets the date and time at which the reservation was processed.
            This accessor is private. """
        self.timestamp_value = value

    @property
    def number_of_seats(self):
        """ Gets the number of seats reserved by the user. """
        return self.number_of_seats_value

    @number_of_seats.setter
    def number_of_seats(self, value):
        """ Sets the number of seats reserved by the user.
            This accessor is private. """
        self.number_of_seats_value = value

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.id