﻿
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import * as Finder from 'Framework/Signum.React/Scripts/Finder'
import { openModal, IModalProps } from 'Framework/Signum.React/Scripts/Modals';
import { ResultTable, FindOptions, FindMode, FilterOption, QueryDescription } from 'Framework/Signum.React/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from 'Framework/Signum.React/Scripts/Signum.Entities'
import * as Reflection from 'Framework/Signum.React/Scripts/Reflection'
import { default as SearchControl, SearchControlProps} from 'Templates/SearchControl/SearchControl'


interface SearchPopupProps extends React.Props<SearchPopup>, IModalProps {
    findOptions: FindOptions;
    findMode: FindMode;
    isMany: boolean;
    title?: string;
}

export default class SearchPopup extends React.Component<SearchPopupProps, { show: boolean }>  {

    constructor(props) {
        super(props);

        this.state = { show: true };
    }

    selectedEntites: Lite<Entity>[] = [];
    okPressed: boolean;

    handleSelectionChanged = (selected: Lite<Entity>[]) => {
        this.selectedEntites = selected;
        this.forceUpdate();
    }

    handleOkClicked = () => {
        this.okPressed = true;
        this.setState({ show: false });
    }

    handleCancelClicked = () => {
        this.okPressed = false;
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited(this.okPressed ? this.selectedEntites : null);
    }

    render() {

        var okEnabled = this.props.isMany ? this.selectedEntites.length > 0 : this.selectedEntites.length == 1;

        return <Modal bsSize="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited}>
            <Modal.Header closeButton={this.props.findMode == FindMode.Explore}>
                { this.props.findMode == FindMode.Find &&
                <div className="btn-toolbar" style={{ float: "right" }}>
                            <button className ="btn btn-primary sf-entity-button sf-close-button sf-ok-button" disabled={!okEnabled} onClick={this.handleOkClicked}>
                            {JavascriptMessage.ok.niceToString() }
                                </button>

                            <button className ="btn btn-default sf-entity-button sf-close-button sf-cancel-button" onClick={this.handleCancelClicked}>{JavascriptMessage.cancel.niceToString() }</button>
                    </div>}
                <h4>
                    <span className="sf-entity-title"> {this.props.title}</span>
                    <a className ="sf-popup-fullscreen" href="#">
                    <span className="glyphicon glyphicon-new-window"></span>
                        </a>
                    </h4>
                </Modal.Header>

            <Modal.Body>
                <SearchControl avoidFullScreenButton={true} findOptions={this.props.findOptions} onSelectionChanged={this.handleSelectionChanged} />
                </Modal.Body>
            </Modal>;
    }

    static open(findOptions: FindOptions, title?: string): Promise<Lite<Entity>> {

        return openModal<Lite<Entity>[]>(<SearchPopup
            findOptions={findOptions}
            findMode={FindMode.Find}
            isMany={false}
            title={title || Reflection.getQueryNiceName(findOptions.queryName) } />)
            .then(a => a ? a[0] : null);
    }

    static openMany(findOptions: FindOptions, title?: string): Promise<Lite<Entity>[]> {

        return openModal<Lite<Entity>[]>(<SearchPopup findOptions={findOptions}
            findMode={FindMode.Find}
            isMany={true}
            title={title || Reflection.getQueryNiceName(findOptions.queryName) } />);
    }
}



