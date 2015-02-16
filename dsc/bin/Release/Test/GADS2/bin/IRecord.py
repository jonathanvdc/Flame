class IRecord:
    """ Describes a generic record. """

    @property
    def key(self):
        """ Gets the record's search key. """
        raise NotImplementedError("Getter of property 'IRecord.key' was not implemented.")