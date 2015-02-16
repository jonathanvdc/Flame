from Ticket import *
from Reservation import *
from Stack import *
from IRecord import *

class Showtime(IRecord):
    """ Describes a showtime at the movie theater. """

    def __init__(self, Id, Location, MoviePlaying, StartTime):
        """ Creates a new instance of a showtime. """
        # Pre:
        # Id must be a valid and unique identifier, the location must be an existing at the theater, the movie must be known to the theater and the start time must correspond to one of the theater's time slots.
        self.number_of_free_seats_value = 0
        self.tickets = Stack()
        self.id = Id
        self.location = Location
        self.movie_playing = MoviePlaying
        self.start_time = StartTime
        self.number_of_free_seats = Location.number_of_seats

    def __str__(self):
        """ Gets the showtime's string representation. """
        reservedSeats = self.location.number_of_seats - self.number_of_free_seats
        present = reservedSeats - self.tickets.count
        return "Showtime #" + str(self.id) + " of " + str(self.movie_playing) + ", " + str(self.location) + ", at " + str(self.timeslot) + " (" + str(self.date) + "), " + str(self.number_of_free_seats) + " free seats, " + str(present) + " people present"

    def make_reservation(self, Id, Request):
        """ Reserves a ticket for this showtime. """
        # Pre:
        # Id should be a unique identifier, and the number of seats in request should be no more than the amount of free seats.
        # The reservation request must always be a reservation for this showtime.
        # Post:
        # If the reservation request demanded more seats than available, None will be returned, and no seats will be reserved.
        # The reservation is in effect considered to be canceled.
        if Request.number_of_seats > self.number_of_free_seats:
            return None
        else:
            i = 0
            while i < Request.number_of_seats:
                self.number_of_free_seats -= 1
                self.tickets.push(Ticket(Request.customer))
                i += 1
            return Reservation(Id, Request.customer, self, Request.timestamp, Request.number_of_seats)

    def has_ticket(self, Customer):
        """ Gets a boolean value that indicates whether the provided customer has a ticket for this showtime. """
        # Post:
        # If the showtime has any tickets associated with it that belong to the given customer, 'True' is returned.
        # Otherwise, 'False' if returned.
        for item in self.tickets:
            if item.customer == Customer:
                return True
        return False

    def redeem_ticket(self, Theater, Customer):
        """ Have one person redeem their ticket and enter the showtime.
            Note that a user who reserved more than one ticket must enter the showtime multiple times, once per ticket. """
        # Pre:
        # The customer must be a user who has an unredeemed ticket for this showtime.
        # The theater must be the theater containing this showtime.
        # Post:
        # If the customer does not have an unredeemed ticket for this showtime, this method does nothing.
        # If, on the other hand, the user does, their ticket is redeemed, and their absence will no longer delay the showtime.
        # If the last ticket was redeemed, the showtime begins, and is be removed from the theater's showtime list.
        if self.has_ticket(Customer):
            tempStorage = Stack()
            while not self.tickets.is_empty:
                item = self.tickets.pop()
                if item.customer == Customer:
                    break
                else:
                    tempStorage.push(item)
            while not tempStorage.is_empty:
                self.tickets.push(tempStorage.pop())
            if self.tickets.is_empty:
                Theater.showtimes.remove(self.id)

    @property
    def location(self):
        """ Gets the auditorium where the showtime will take place. """
        return self.location_value

    @location.setter
    def location(self, value):
        """ Sets the auditorium where the showtime will take place.
            This accessor is private. """
        self.location_value = value

    @property
    def start_time(self):
        """ Gets the date and time for which this showtime is scheduled. """
        return self.start_time_value

    @start_time.setter
    def start_time(self, value):
        """ Sets the date and time for which this showtime is scheduled.
            This accessor is private. """
        self.start_time_value = value

    @property
    def id(self):
        """ Gets the showtime's unique identifier. """
        return self.id_value

    @id.setter
    def id(self, value):
        """ Sets the showtime's unique identifier.
            This accessor is private. """
        self.id_value = value

    @property
    def movie_playing(self):
        """ Gets the movie that will play at the showtime. """
        return self.movie_playing_value

    @movie_playing.setter
    def movie_playing(self, value):
        """ Sets the movie that will play at the showtime.
            This accessor is private. """
        self.movie_playing_value = value

    @property
    def number_of_free_seats(self):
        """ Gets the number of remaining free seats for this showtime. """
        return self.number_of_free_seats_value

    @number_of_free_seats.setter
    def number_of_free_seats(self, value):
        """ Sets the number of remaining free seats for this showtime.
            This accessor is private. """
        self.number_of_free_seats_value = value

    @property
    def timeslot(self):
        """ Gets the movie's starting time. """
        return self.start_time.time_of_day

    @property
    def date(self):
        """ Gets the date assigned to the showtime. """
        return self.start_time.date

    @property
    def auditorium_index(self):
        """ Gets the index of the auditorium where the showtime will take place. """
        return self.location.index

    @property
    def movie_id(self):
        """ Gets the identifier of the movie that's playing at the showtime instance. """
        return self.movie_playing.id

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.id