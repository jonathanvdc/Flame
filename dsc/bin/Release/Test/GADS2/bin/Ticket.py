from IRecord import *

class Ticket(IRecord):
    """ Describes a ticket at a movie theater. """

    def __init__(self, Customer):
        """ Creates a new ticket instance based on the customer it belongs to. """
        self.customer = Customer

    @property
    def customer(self):
        """ Gets the user that is associated with this ticket. """
        return self.customer_value

    @customer.setter
    def customer(self, value):
        """ Sets the user that is associated with this ticket.
            This accessor is private. """
        self.customer_value = value

    @property
    def customer_id(self):
        """ Gets the identifier of the user that is associated with this ticket. """
        return self.customer.id

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.customer_id