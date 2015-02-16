from LinkedList import *
from IReadOnlyCollection import *

class Stack(IReadOnlyCollection):
    """ Represents a generic stack. """
    # Remarks:
    # T is the type of item on the stack.

    def __init__(self, dataContainer = None):
        """ Creates a new stack instance that uses the specified list to store its data. """
        if dataContainer is None:
            self.data_container = None
            self.data_container = LinkedList()
            return
        self.data_container = dataContainer

    def push(self, Item):
        """ Pushes an item on the stack. """
        self.data_container.insert(0, Item)

    def pop(self):
        """ Pops the item at the top of the stack. """
        # Pre:
        # The stack may not be empty.
        # Post:
        # If the stack was empty, the stack's state will not change, and None will be returned.
        if self.is_empty:
            return None
        value = self.data_container[0]
        self.data_container.remove_at(0)
        return value

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.data_container.__iter__()

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates whether the stack is empty or not. """
        return self.count == 0

    @property
    def count(self):
        """ Gets the number of items on the stack. """
        return self.data_container.count

    @property
    def top(self):
        """ Peeks at the item at the top of the stack, without removing it. """
        # Pre:
        # The stack may not be empty.
        # Post:
        # If the stack was empty, None will be returned.
        if self.is_empty:
            return None
        else:
            return self.data_container[0]