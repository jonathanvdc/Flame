from ArrayList import *
from Queue import *
from SwapList import *

class ReservationManager:
    """ Manages reservations for a movie theater. """

    def __init__(self):
        """ Creates a new instance of the reservation manager. """
        self.current_id = 0
        self.requests = Queue()
        self.all_reservations = SwapList(ArrayList())

    def queue_reservation(self, Request):
        """ Queues a reservation for a showtime for processing. """
        self.requests.enqueue(Request)

    def process_reservations(self):
        """ Processes all queued reservations and returns a read-only list containing the newly made reservations. """
        results = ArrayList()
        while not self.requests.is_empty:
            request = self.requests.dequeue()
            reserv = request.showtime.make_reservation(self.current_id, request)
            if reserv is not None:
                self.current_id += 1
                self.all_reservations.add(reserv)
                results.add(reserv)
        return results

    @property
    def reservations(self):
        """ Gets the movie theater's processed and accepted reservations. """
        return self.all_reservations