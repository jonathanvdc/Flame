class ReservationRequest:
    """ A request for a reservation.
        Reservation requests are not permanent and do not have an ID. """
    # Remarks:
    # Reservation requests are intended to be an easy way to create a reservation: the client only has to fill in the data fields, and need not concern itself with the number of remaining seats or the hassle of generating a unique identifier.
    # Also, their none-permanent nature allows them to be disposed of without consequences if anything goes wrong while processing the reservation.

    def __init__(self, Customer, Showtime, NumberOfSeats, Timestamp):
        """ Creates a new reservation request. """
        self.customer = Customer
        self.showtime = Showtime
        self.number_of_seats = NumberOfSeats
        self.timestamp = Timestamp

    @property
    def number_of_seats(self):
        """ Gets the number of seats this reservation requests. """
        return self.number_of_seats_value

    @number_of_seats.setter
    def number_of_seats(self, value):
        """ Sets the number of seats this reservation requests.
            This accessor is private. """
        self.number_of_seats_value = value

    @property
    def customer(self):
        """ Gets the user who placed this request. """
        return self.customer_value

    @customer.setter
    def customer(self, value):
        """ Sets the user who placed this request.
            This accessor is private. """
        self.customer_value = value

    @property
    def timestamp(self):
        """ Gets this reservation's timestamp. """
        return self.timestamp_value

    @timestamp.setter
    def timestamp(self, value):
        """ Sets this reservation's timestamp.
            This accessor is private. """
        self.timestamp_value = value

    @property
    def showtime(self):
        """ Gets the showtime request this reservation was placed for. """
        return self.showtime_value

    @showtime.setter
    def showtime(self, value):
        """ Sets the showtime request this reservation was placed for.
            This accessor is private. """
        self.showtime_value = value