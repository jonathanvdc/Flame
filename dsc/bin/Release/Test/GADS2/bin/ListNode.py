from ListNode import *

class ListNode:
    """ Describes a node in a linked list. """
    # Remarks:
    # This class provides low-level access to the internal workings of a linked list.

    def __init__(self, Value):
        """ Creates a new linked list node instance from the specified value. """
        # Post:
        # Value will populate the linked list's Value property.
        self.successor_value = None
        self.value = Value

    def insert_after(self, Value):
        """ Inserts a node containing the provided value after this node. """
        # Post:
        # Creates a new node containing the provided value, sets its successor to this node's successor, and sets this node's successor to the newly created node.
        # Remarks:
        # This operation corresponds to (part of) a list insert for linked lists.
        nextVal = ListNode(Value)
        nextVal.successor = self.successor
        self.successor = nextVal

    @property
    def value(self):
        """ Gets the value contained in the list node. """
        return self.value_value

    @value.setter
    def value(self, value):
        """ Sets the value contained in the list node. """
        self.value_value = value

    @property
    def successor(self):
        """ Gets the list node's successor node. """
        # Post:
        # Gets the successor, if any.
        # Otherwise, returns None.
        return self.successor_value

    @successor.setter
    def successor(self, value):
        """ Sets the list node's successor node. """
        # Pre:
        # Sets the list node's successor to value, which may be either None or a ListNode<T>.
        self.successor_value = value

    @property
    def tail(self):
        """ Gets the "tail" node of this linked chain. """
        # Post:
        # The tail of a list node is defined recursively as follows.
        # If the current node has no successor, return the current node.
        # Otherwise, return the current node's successor's tail.
        if self.successor is None:
            return self
        else:
            return self.successor.tail